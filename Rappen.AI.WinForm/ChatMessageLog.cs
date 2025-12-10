using Microsoft.Extensions.AI;
using System;
using System.Drawing;
using System.Windows.Forms;
using Rappen.XRM.Helpers.Extensions;

namespace Rappen.AI.WinForm
{
    internal class ChatMessageLog : ChatMessage
    {
        private const int messageWidthPercent = 70;
        private Panel containerPanel;
        private Panel contentPanel;
        private RichTextBox messageTextBox;
        private TextBox stampTextBox;

        public bool OnlyInfo { get; }

        public readonly DateTime TimeStamp;
        public readonly string Sender;

        public ChatMessageLog()
        {
            TimeStamp = DateTime.Now;
        }

        public ChatMessageLog(ChatMessage message) : base(message.Role, message.Text)
        {
            TimeStamp = DateTime.Now;
        }

        public ChatMessageLog(ChatRole role, string content, string sender, bool onlyinfo) : base(role, content)
        {
            OnlyInfo = onlyinfo;
            TimeStamp = DateTime.Now;
            Sender = string.IsNullOrEmpty(sender) ? role.ToString() : sender;
        }

        public override string ToString() => $"{TimeStamp:G} - {Sender}{(OnlyInfo ? " - only for info" : "")}{Environment.NewLine}{Text}{Environment.NewLine}";

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
            var backcolor = OnlyInfo ? ChangeColorBrightness(BackColor, 0.5f) : BackColor;

            // Create the container panel and content panel
            containerPanel = new Panel
            {
                Tag = this,
                Padding = new Padding(2, 4, 2, 0),
                Dock = DockStyle.Bottom,
            };
            containerPanel.Resize += Panel_Resize;

            // Add the container panel to contain the message and stamp
            contentPanel = new Panel
            {
                BackColor = backcolor,
                Padding = new Padding(4),
                Width = (containerPanel.Width * messageWidthPercent) / 100,
                Height = 60,
                Dock = DockStyle
            };
            containerPanel.Controls.Add(contentPanel);

            // Create the message text box
            messageTextBox = new RichTextBox
            {
                BackColor = backcolor,
                ForeColor = ForeColor,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = true,
                ReadOnly = true,
            };

            // Check if we need to apply RTF formatting
            if (ContainsMarkdown(Text))
            {
                // Generate the RTF content
                var rtfContent = MarkdownToRtfConverter.Convert(Text, ForeColor, backcolor);

                // Defer RTF assignment until the control has a window handle
                void ApplyRtf(object s, EventArgs ev)
                {
                    messageTextBox.HandleCreated -= ApplyRtf;
                    messageTextBox.Rtf = rtfContent;
                }

                // If handle already exists, apply immediately; otherwise wait for HandleCreated
                if (messageTextBox.IsHandleCreated)
                {
                    messageTextBox.Rtf = rtfContent;
                }
                else
                {
                    messageTextBox.HandleCreated += ApplyRtf;
                    // Set plain text as initial content (will be replaced when handle is created)
                    messageTextBox.Text = Text ?? "(no message)";
                }
            }
            else
            {
                messageTextBox.Text = Text ?? "(no message)";
            }

            messageTextBox.ContentsResized += Message_ContentsResized;
            messageTextBox.LinkClicked += Message_LinkClicked;

            // Create the stamp text box
            stampTextBox = new TextBox
            {
                BackColor = backcolor,
                ForeColor = ForeColor,
                BorderStyle = BorderStyle.None,
                Text = $"{Sender} @ {TimeStamp:T}",
                TextAlign = HorizontalAlignment.Right,
                Dock = DockStyle.Bottom,
                ReadOnly = true,
            };

            // Adding message and stamp text boxes to the content panel
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

        private void Message_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.LinkText,
                UseShellExecute = true
            });
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

        private void copyMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText($"{Text}{Environment.NewLine}// {Sender} @ {TimeStamp:T}");
        }

        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = color.R;
            float green = color.G;
            float blue = color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor; // Reduce brightness
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red; // Increase brightness
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        private static bool ContainsMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            // Check for common markdown patterns:
            // - Bold: **text**
            // - Italic: *text* (single asterisk not at line start)
            // - Inline code: `code`
            // - Code blocks: ```
            // - Headers: # at line start
            // - Bullet lists: - or * at line start followed by space
            return text.Contains("**") ||
                   text.Contains("`") ||
                   text.Contains("\n# ") ||
                   text.StartsWith("# ") ||
                   text.Contains("\n- ") ||
                   text.StartsWith("- ") ||
                   text.Contains("\n* ") ||
                   text.StartsWith("* ") ||
                   System.Text.RegularExpressions.Regex.IsMatch(text, @"(?<!\*)\*(?!\s)[^*\n]+(?<!\s)\*(?!\*)");
        }
    }
}