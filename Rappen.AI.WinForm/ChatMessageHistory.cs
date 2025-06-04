using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.AI.WinForm
{
    public class ChatMessageHistory
    {
        private Panel parent;
        private List<ChatLog> chatMessages;

        internal static Color AssistansBackgroundColor = Color.FromArgb(0, 99, 255);
        internal static Color AssistansTextColor = Color.FromArgb(0, 66, 173);
        internal static Color UserBackgroundColor = Color.FromArgb(0, 99, 255);
        internal static Color UserTextColor = Color.FromArgb(255, 255, 0);
        internal static Color OtherBackgroundColor = Color.LightGray;
        internal static Color OtherTextColor = Color.Black;

        public IEnumerable<ChatMessage> Messages => chatMessages;

        public ChatMessageHistory(Panel parent)
        {
            this.parent = parent;
            chatMessages = new List<ChatLog>();
        }

        public void Add(ChatRole role, string content, bool hidden)
        {
            var chatLog = new ChatLog(role, content);
            if (!string.IsNullOrWhiteSpace(content))
            {
                chatMessages.Add(chatLog);
                if (!hidden)
                {
                    parent.Controls.Add(chatLog.Panel);
                    parent.VerticalScroll.Value = parent.VerticalScroll.Maximum;
                    parent.PerformLayout();
                }
            }
        }

        public void Add(ChatResponse response) => response.Messages.ToList().ForEach(x => Add(x));

        private void Add(ChatMessage message) => Add(message.Role, message.Text, false);
    }

    public class ChatLog : ChatMessage
    {
        private Panel panel;
        private Panel content;
        private RichTextBox message;
        private TextBox stamp;

        internal DateTime timestamp;

        public ChatLog()
        {
            timestamp = DateTime.Now;
        }

        public ChatLog(ChatMessage message) : base(message.Role, message.Text)
        {
            timestamp = DateTime.Now;
        }

        public ChatLog(ChatRole role, string content) : base(role, content)
        {
            timestamp = DateTime.Now;
        }

        public Panel Panel
        {
            get
            {
                if (panel == null)
                {
                    GetPanel();
                }
                return panel;
            }
        }

        public HorizontalAlignment Position =>
            Role == ChatRole.Assistant ? HorizontalAlignment.Left :
            Role == ChatRole.User ? HorizontalAlignment.Right :
            HorizontalAlignment.Center;

        public DockStyle DockStyle =>
            Role == ChatRole.Assistant ? DockStyle.Left :
            Role == ChatRole.User ? DockStyle.Right :
            DockStyle.Top;

        public Color BackColor =>
            Role == ChatRole.User ? ChatMessageHistory.UserBackgroundColor :
            Role == ChatRole.Assistant ? ChatMessageHistory.AssistansBackgroundColor :
            ChatMessageHistory.OtherBackgroundColor;

        public Color ForeColor =>
            Role == ChatRole.User ? ChatMessageHistory.UserTextColor :
            Role == ChatRole.Assistant ? ChatMessageHistory.AssistansTextColor :
            ChatMessageHistory.OtherTextColor;

        private void GetPanel()
        {
            panel = new Panel
            {
                Tag = this,
                Padding = new Padding(2, 4, 2, 0),
                Dock = DockStyle.Bottom,
            };
            panel.Resize += Panel_Resize;
            var width = (int)(panel.Width * 0.7);
            content = new Panel
            {
                BackColor = BackColor,
                Padding = new Padding(4),
                Width = width,
                Height = 60,
                Dock = DockStyle
            };
            panel.Controls.Add(content);
            message = new RichTextBox
            {
                BackColor = BackColor,
                ForeColor = ForeColor,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = true,
            };
            if (false && this.Text.Contains("`"))
            {
                //      message.Rtf = MarkdownToRtfConverter.Convert(this.Text);
            }
            else
            {
                message.Text = this.Text ?? "<no message>";
            }
            message.ContentsResized += Message_ContentsResized;
            stamp = new TextBox
            {
                BackColor = BackColor,
                ForeColor = ForeColor,
                BorderStyle = BorderStyle.None,
                Text = $"{Role.Value} @ {timestamp:T}",
                TextAlign = HorizontalAlignment.Right, // Position,
                Dock = DockStyle.Bottom
            };
            content.Controls.Add(stamp);
            content.Controls.Add(message);
        }

        private void Message_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            if (sender == message)
            {
                var cntheight =
                    e.NewRectangle.Height +                         // text height
                    stamp.Height +                                  // stamp height
                    panel.Padding.Top + panel.Padding.Bottom +      // panel padding
                    content.Padding.Top + content.Padding.Bottom;   // contect padding
                panel.Height = cntheight;
            }
        }

        private void Panel_Resize(object sender, EventArgs e)
        {
            if (!(sender is Panel panel))
            {
                return;
            }
            var width = panel.Width;
            content.Width = (int)(width * 0.7);
        }
    }
}