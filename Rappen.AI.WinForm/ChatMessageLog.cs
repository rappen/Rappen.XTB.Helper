using Microsoft.Extensions.AI;
using Rappen.XRM.Helpers.Extensions.Markdown;
using System;
using System.Drawing;
using System.Windows.Forms;

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

        public string ToMarkdown() => !string.IsNullOrWhiteSpace(Text) ? $"##### {TimeStamp:G} - {Sender}{(OnlyInfo ? " *(only for info)*" : "")}{Environment.NewLine}{Text}{Environment.NewLine}" : "";

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

            containerPanel = new Panel
            {
                Tag = this,
                Padding = new Padding(2, 4, 2, 0),
                Dock = DockStyle.Bottom,
            };
            containerPanel.Resize += Panel_Resize;
            containerPanel.MouseWheel += Child_MouseWheel; // forward scroll

            contentPanel = new Panel
            {
                BackColor = backcolor,
                Padding = new Padding(4),
                Width = (containerPanel.Width * messageWidthPercent) / 100,
                Height = 60,
                Dock = DockStyle
            };
            contentPanel.MouseWheel += Child_MouseWheel; // forward scroll
            containerPanel.Controls.Add(contentPanel);

            messageTextBox = new RichTextBox
            {
                BackColor = backcolor,
                ForeColor = ForeColor,
                Font = ChatMessageHistory.Font,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ScrollBars = RichTextBoxScrollBars.None,
                WordWrap = true,
                ReadOnly = true,
            };
            messageTextBox.HandleCreated += MessageTextBox_HandleCreated;
            messageTextBox.ContentsResized += Message_ContentsResized;
            messageTextBox.LinkClicked += Message_LinkClicked;
            messageTextBox.MouseWheel += Child_MouseWheel; // forward scroll

            stampTextBox = new TextBox
            {
                BackColor = backcolor,
                ForeColor = ForeColor,
                Font = ChatMessageHistory.Font,
                BorderStyle = BorderStyle.None,
                Text = $"{Sender} @ {TimeStamp:T}",
                TextAlign = HorizontalAlignment.Right,
                Dock = DockStyle.Bottom,
                ReadOnly = true,
            };
            stampTextBox.Height = stampTextBox.PreferredHeight;
            stampTextBox.MouseWheel += Child_MouseWheel; // forward scroll

            contentPanel.Controls.Add(stampTextBox);
            contentPanel.Controls.Add(messageTextBox);

            if (Text.IsMarkdown())
            {
                var rtfContent = Text.ConvertMarkdownToRtf(ForeColor, backcolor, ChatMessageHistory.Font);

                void ApplyRtf(object s, EventArgs ev)
                {
                    messageTextBox.HandleCreated -= ApplyRtf;
                    messageTextBox.Rtf = rtfContent;
                    ApplyWrapMarginIfNeeded();
                }

                if (messageTextBox.IsHandleCreated)
                {
                    messageTextBox.Rtf = rtfContent;
                    ApplyWrapMarginIfNeeded();
                }
                else
                {
                    messageTextBox.HandleCreated += ApplyRtf;
                    messageTextBox.Text = Text ?? "(no message)";
                }
            }
            else
            {
                messageTextBox.Text = Text ?? "(no message)";
            }
        }

        private void Message_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            if (sender != messageTextBox)
            {
                return;
            }

            var stampHeight = Math.Max(stampTextBox.Height, stampTextBox.PreferredHeight);
            var containerHeight =
                e.NewRectangle.Height +
                stampHeight +
                containerPanel.Padding.Top +
                containerPanel.Padding.Bottom +
                contentPanel.Padding.Top +
                contentPanel.Padding.Bottom;

            if (containerPanel.Height != containerHeight)
            {
                containerPanel.Height = containerHeight;
            }

            if (contentPanel.Dock == DockStyle.Top)
            {
                var contentHeight = containerHeight - containerPanel.Padding.Top - containerPanel.Padding.Bottom;
                if (contentPanel.Height != contentHeight)
                {
                    contentPanel.Height = contentHeight;
                }
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

            if (Role == ChatRole.System)
            {
                var horizontalPadding = (width * (100 - messageWidthPercent)) / 200;
                var padding = new Padding(horizontalPadding, 4, horizontalPadding, 0);

                if (!containerPanel.Padding.Equals(padding))
                {
                    containerPanel.Padding = padding;
                }

                var contentWidth = Math.Max(0, width - padding.Left - padding.Right);
                if (contentPanel.Width != contentWidth)
                {
                    contentPanel.Width = contentWidth;
                }

                if (contentPanel.Left != padding.Left)
                {
                    contentPanel.Left = padding.Left;
                }
            }
            else
            {
                var padding = new Padding(2, 4, 2, 0);

                if (!containerPanel.Padding.Equals(padding))
                {
                    containerPanel.Padding = padding;
                }

                var contentWidth = (width * messageWidthPercent) / 100;
                if (contentPanel.Width != contentWidth)
                {
                    contentPanel.Width = contentWidth;
                }

                var left = Role == ChatRole.Assistant ? 0 : width - contentWidth;
                if (contentPanel.Left != left)
                {
                    contentPanel.Left = left;
                }
            }

            ApplyWrapMarginIfNeeded();
        }

        private void MessageTextBox_HandleCreated(object sender, EventArgs e)
        {
            ApplyWrapMarginIfNeeded();
        }

        private void ApplyWrapMarginIfNeeded()
        {
            if (messageTextBox == null || !messageTextBox.IsHandleCreated)
            {
                return;
            }

            var wrapWidth = messageTextBox.ClientSize.Width;
            if (wrapWidth <= 1)
            {
                return;
            }

            var rightMargin = wrapWidth - 1;
            if (messageTextBox.RightMargin != rightMargin)
            {
                messageTextBox.RightMargin = rightMargin;
            }
        }

        internal void EnsureWrappedLayout()
        {
            if (containerPanel == null || contentPanel == null || messageTextBox == null)
            {
                return;
            }

            Panel_Resize(containerPanel, EventArgs.Empty);
            ApplyWrapMarginIfNeeded();
        }

        private void Child_MouseWheel(object sender, MouseEventArgs e)
        {
            // Find the first scrollable parent (your panAiConversation)
            var parent = containerPanel?.Parent as ScrollableControl;
            while (parent != null && !parent.AutoScroll)
            {
                parent = parent.Parent as ScrollableControl;
            }

            if (parent == null)
            {
                return;
            }

            // Manually adjust vertical scroll value
            var vs = parent.VerticalScroll;
            var newValue = vs.Value - Math.Sign(e.Delta) * vs.SmallChange * 12;
            if (newValue < vs.Minimum)
            {
                newValue = vs.Minimum;
            }

            if (newValue > vs.Maximum)
            {
                newValue = vs.Maximum;
            }

            vs.Value = newValue;
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
    }
}