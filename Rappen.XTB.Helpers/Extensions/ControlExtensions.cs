using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Extensions
{
    public static class ControlExtensions
    {
        public static List<T> AllChildren<T>(this Control control) where T : Control
        {
            var result = new List<T>();
            if (control is T)
            {
                result.Add((T)control);
            }
            foreach (Control child in control.Controls)
            {
                result.AddRange(child.AllChildren<T>());
            }
            return result;
        }

        public static void HighlightFilter(this RichTextBox textbox, string text, bool matchcases, Color bg, Color fr)
        {
            if (textbox == null)
            {
                throw new ArgumentNullException(nameof(textbox));
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Preserve user selection/caret.
            var originalSelectionStart = textbox.SelectionStart;
            var originalSelectionLength = textbox.SelectionLength;

            var length = text.Length;
            var start = 0;

            // IMPORTANT:
            // RichTextBox.Find() does not necessarily move the SelectionStart.
            // If we don’t explicitly select the found match, SelectionStart can stay the same,
            // and subsequent Find() calls can return the same position, causing an infinite loop.
            while (start <= textbox.TextLength - length)
            {
                var pos = textbox.Find(text, start, matchcases ? RichTextBoxFinds.MatchCase : RichTextBoxFinds.None);
                if (pos < 0)
                {
                    break;
                }

                textbox.Select(pos, length);
                textbox.SelectionBackColor = bg;
                textbox.SelectionColor = fr;

                // Ensure forward progress even if something odd happens.
                start = pos + Math.Max(1, length);
            }

            textbox.Select(originalSelectionStart, originalSelectionLength);
        }
    }
}