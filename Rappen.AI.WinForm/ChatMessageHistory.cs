﻿using Microsoft.Extensions.AI;
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
        internal static Color AssistansBackgroundColor = Color.FromArgb(0, 99, 255);
        internal static Color AssistansTextColor = Color.FromArgb(0, 66, 173);
        internal static Color UserBackgroundColor = Color.FromArgb(0, 99, 255);
        internal static Color UserTextColor = Color.FromArgb(255, 255, 0);
        internal static Color OtherBackgroundColor = Color.LightGray;
        internal static Color OtherTextColor = Color.Black;
        internal static Color BackColor = Color.White;
        internal static Color WaitingBackColor = Color.LightGray;

        private Panel parent;
        private readonly string user;
        private readonly DateTime starttime;
        private readonly Timer timer;
        private int timerno = 0;
        private TextBox waitingtxt;

        internal readonly string Provider;
        internal readonly string Endpoint;
        internal readonly string Model;
        internal readonly string ApiKey;

        internal List<ChatMessageLog> Messages { get; private set; }

        internal ChatResponseList Responses { get; private set; }

        public ChatMessageHistory(Panel parent, string provider, string endpoint, string model, string apikey, string user)
        {
            this.parent = parent;
            Provider = provider;
            Endpoint = endpoint;
            Model = model;
            ApiKey = apikey;
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
            Messages = new List<ChatMessageLog>();
            Responses = new ChatResponseList();
            this.parent.BackColor = BackColor;
            this.parent.Controls.Clear();
        }

        public override string ToString()
        {
            return $"Started:  {starttime:G}{Environment.NewLine}Provider: {Provider}{Environment.NewLine}Model:    {Model}" +
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
            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                File.WriteAllText(file, ToString());
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show($"Could not save chat history to file:{Environment.NewLine}  {file}{Environment.NewLine}{Environment.NewLine}{ex.Message}", "Error saving chat history", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string Save(string folder, string tool)
        {
            if (Messages?.Any() != true)
            {
                return null;
            }
            var path = Path.Combine(folder, $"{tool} AI Chat\\{Provider ?? "AI"} {starttime:yyyyMMdd HHmmssfff}.txt");
            Save(path);
            return path;
        }

        public void Add(ChatRole role, string content, bool hidden)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }
            var sender = role == ChatRole.User ? user : role == ChatRole.Assistant ? Provider : "";
            var chatLog = new ChatMessageLog(role, content.Trim(), sender);
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

        public long? TokensOut => Responses?.TokensOut;

        public long? TokensIn => Responses?.TokensIn;

        public long? TokensTotal => Responses?.TokensTotal;

        private void Timer_Tick(object sender, EventArgs e)
        {
            timerno = timerno < 15 ? timerno + 1 : 0;
            var pos = timerno <= 10 ? timerno : 0;
            waitingtxt.Text = new String('o', pos) + (timerno <= 10 ? '0' : 'o') + new String('o', 10 - pos);
        }
    }

    public class ChatResponseList : List<ChatResponse>
    {
        public long TokensOut => this.Sum(r => r.Usage.OutputTokenCount) ?? 0;
        public long TokensIn => this.Sum(r => r.Usage.InputTokenCount) ?? 0;
        public long TokensTotal => this.Sum(r => r.Usage.TotalTokenCount) ?? 0;

        public string UsageToString() => $"Answers: {Count} Tokens: Out {TokensOut}, In {TokensIn}, Total {TokensTotal}";
    }
}