using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.AI.WinForm
{
    public class ChatMessageHistory
    {
        private Panel parent;
        private readonly string supplier;
        private readonly string model;
        private readonly string user;
        private readonly DateTime starttime;
        private readonly Timer timer;
        private int timerno = 0;
        private TextBox waitingtxt;

        internal static Color AssistansBackgroundColor = Color.FromArgb(0, 99, 255);
        internal static Color AssistansTextColor = Color.FromArgb(0, 66, 173);
        internal static Color UserBackgroundColor = Color.FromArgb(0, 99, 255);
        internal static Color UserTextColor = Color.FromArgb(255, 255, 0);
        internal static Color OtherBackgroundColor = Color.LightGray;
        internal static Color OtherTextColor = Color.Black;
        internal static Color BackColor = Color.White;
        internal static Color WaitingBackColor = Color.LightGray;

        public List<ChatLog> Messages { get; private set; }

        public ChatResponseList Responses { get; private set; }

        public ChatMessageHistory(Panel parent, string supplier, string model, string user = null)
        {
            this.parent = parent;
            this.supplier = supplier;
            this.model = model;
            this.user = user;
            timer = new Timer
            {
                Interval = 100,
                Enabled = false,
            };
            timer.Tick += Timer_Tick;
            waitingtxt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = WaitingBackColor,
                ForeColor = Color.Black,
                Dock = DockStyle.Bottom,
                TextAlign = HorizontalAlignment.Center,
            };
            starttime = DateTime.Now;
            Messages = new List<ChatLog>();
            Responses = new ChatResponseList();
            this.parent.BackColor = BackColor;
            this.parent.Controls.Clear();
        }

        public override string ToString()
        {
            return $"Started:  {starttime:G}{Environment.NewLine}Supplier: {supplier}{Environment.NewLine}Model:    {model}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, Messages.Select(m => m.ToString()))}";
        }

        public void Initialize(string intro)
        {
            if (Messages?.Any(m => m.Role == ChatRole.System) != true && !string.IsNullOrWhiteSpace(intro))
            {
                Add(ChatRole.System, intro, true);
            }
        }

        public bool Initialized => Messages?.Any(m => m.Role == ChatRole.System) == true;

        public void Save(string file)
        {
            if (Messages?.Any() != true)
            {
                return;
            }
            var folder = Path.GetDirectoryName(file);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllText(file, ToString());
            //XmlSerializerHelper.SerializeToFile(Messages.Select(m=>m.ser), Path.Combine(folder, $"{tool} AI Chat {starttime:yyyyMMdd HHmmssfff}.xml"));
        }

        public string Save(string folder, string tool)
        {
            if (Messages?.Any() != true)
            {
                return null;
            }
            var path = Path.Combine(folder, $"{tool} AI Chat\\{supplier ?? "AI"} {starttime:yyyyMMdd HHmmssfff}.txt");
            Save(path);
            return path;
        }

        public void Add(ChatRole role, string content, bool hidden)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }
            var sender = role == ChatRole.User ? user : role == ChatRole.Assistant ? supplier : "";
            var chatLog = new ChatLog(role, content.Trim(), sender);
            Messages.Add(chatLog);
            if (!hidden)
            {
                parent.Controls.Add(chatLog.Panel);
                parent.VerticalScroll.Value = parent.VerticalScroll.Maximum;
                parent.PerformLayout();
            }
        }

        public void Add(ChatResponse response)
        {
            Responses.Add(response);
            response.Messages.ToList().ForEach(x => Add(x));
        }

        public void Add(ChatResponse response, bool hidden)
        {
            Responses.Add(response);
            response.Messages.ToList().ForEach(x => Add(x, hidden));
        }

        private void Add(ChatMessage message) => Add(message.Role, message.Text, false);

        private void Add(ChatMessage message, bool hidden) => Add(message.Role, message.Text, hidden);

        public bool HasDialog =>
            Messages?.Any(m => m.Role == ChatRole.User) == true &&
            Messages?.Any(m => m.Role == ChatRole.Assistant) == true;

        public bool IsRunning
        {
            get => parent.BackColor == WaitingBackColor;
            set
            {
                MethodInvoker mi = () =>
                {
                    parent.BackColor = value ? WaitingBackColor : BackColor;
                    if (value)
                    {
                        parent.Controls.Remove(waitingtxt);
                        parent.Controls.Add(waitingtxt);
                        waitingtxt.Text = $"oooooooooo";
                        parent.VerticalScroll.Value = parent.VerticalScroll.Maximum;
                        parent.PerformLayout();
                        timer.Start();
                    }
                    else
                    {
                        timer.Stop();
                        parent.Controls.Remove(waitingtxt);
                        parent.VerticalScroll.Value = parent.VerticalScroll.Maximum;
                        parent.PerformLayout();
                    }
                };
                if (parent.InvokeRequired) parent.Invoke(mi); else mi();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timerno = timerno < 15 ? timerno + 1 : 0;
            var pos = timerno <= 10 ? timerno : 0;
            waitingtxt.Text = new String('o', pos) + (timerno <= 10 ? '0' : 'o') + new String('o', 10 - pos);
        }
    }

    public class ChatLog : ChatMessage
    {
        private const int messageWidthPercent = 70;
        private Panel containerPanel;
        private Panel contentPanel;
        private RichTextBox messageTextBox;
        private TextBox stampTextBox;

        public readonly DateTime TimeStamp;
        public readonly string Sender;

        public ChatLog()
        {
            TimeStamp = DateTime.Now;
        }

        public ChatLog(ChatMessage message) : base(message.Role, message.Text)
        {
            TimeStamp = DateTime.Now;
        }

        public ChatLog(ChatRole role, string content, string sender) : base(role, content)
        {
            TimeStamp = DateTime.Now;
            Sender = string.IsNullOrEmpty(sender) ? role.ToString() : sender;
        }

        public override string ToString() => $"{TimeStamp:G} - {Sender}{Environment.NewLine}{Text}{Environment.NewLine}";

        internal Panel Panel
        {
            get
            {
                if (containerPanel == null)
                {
                    GetPanel();
                }
                return containerPanel;
            }
        }

        private HorizontalAlignment Position =>
            Role == ChatRole.Assistant ? HorizontalAlignment.Left :
            Role == ChatRole.User ? HorizontalAlignment.Left :
            HorizontalAlignment.Center;

        private DockStyle DockStyle =>
            Role == ChatRole.Assistant ? DockStyle.Left :
            Role == ChatRole.User ? DockStyle.Right :
            DockStyle.Top;

        private Color BackColor =>
            Role == ChatRole.User ? ChatMessageHistory.UserBackgroundColor :
            Role == ChatRole.Assistant ? ChatMessageHistory.AssistansBackgroundColor :
            ChatMessageHistory.OtherBackgroundColor;

        private Color ForeColor =>
            Role == ChatRole.User ? ChatMessageHistory.UserTextColor :
            Role == ChatRole.Assistant ? ChatMessageHistory.AssistansTextColor :
            ChatMessageHistory.OtherTextColor;

        private void GetPanel()
        {
            containerPanel = new Panel
            {
                Tag = this,
                Padding = new Padding(2, 4, 2, 0),
                Dock = DockStyle.Bottom,
            };
            containerPanel.Resize += Panel_Resize;
            contentPanel = new Panel
            {
                BackColor = BackColor,
                Padding = new Padding(4),
                Width = (containerPanel.Width * messageWidthPercent) / 100,
                Height = 60,
                Dock = DockStyle
            };
            containerPanel.Controls.Add(contentPanel);
            messageTextBox = new RichTextBox
            {
                BackColor = BackColor,
                ForeColor = ForeColor,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = true,
            };
            if (false && Text.Contains("`"))
            {
                //messageTextBox.Rtf = MarkdownToRtfConverter.Convert(this.Text);
            }
            else
            {
                messageTextBox.Text = Text ?? "(no message)";
            }
            messageTextBox.ContentsResized += Message_ContentsResized;
            stampTextBox = new TextBox
            {
                BackColor = BackColor,
                ForeColor = ForeColor,
                BorderStyle = BorderStyle.None,
                Text = $"{Sender} @ {TimeStamp:T}",
                TextAlign = HorizontalAlignment.Right,
                Dock = DockStyle.Bottom
            };
            contentPanel.Controls.Add(stampTextBox);
            contentPanel.Controls.Add(messageTextBox);
        }

        private void Message_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            if (sender == messageTextBox)
            {
                var cntheight =
                    e.NewRectangle.Height +         // text height
                    stampTextBox.Height +           // stamp height
                    containerPanel.Padding.Top +    // panel padding
                    containerPanel.Padding.Bottom +
                    contentPanel.Padding.Top +      // content padding
                    contentPanel.Padding.Bottom;
                containerPanel.Height = cntheight;
            }
        }

        private void Panel_Resize(object sender, EventArgs e)
        {
            if (!(sender is Panel panel))
            {
                return;
            }
            var width = panel.Width;
            contentPanel.Width = (width * messageWidthPercent) / 100;
            contentPanel.Left = Role == ChatRole.Assistant ? 0 :
                Role == ChatRole.User ? width - contentPanel.Width :
                (width - contentPanel.Width) / 2;
        }
    }

    public class ChatResponseList : List<ChatResponse>
    {
        public string UsageToString() => $"Answers: {Count} Tokens: In {this.Sum(r => r.Usage.InputTokenCount)}, Out {this.Sum(r => r.Usage.OutputTokenCount)}, Total {this.Sum(r => r.Usage.TotalTokenCount)}";
    }
}