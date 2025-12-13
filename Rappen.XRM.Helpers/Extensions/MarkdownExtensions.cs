using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace Rappen.XRM.Helpers.Extensions.Markdown
{
    /// <summary>
    /// Converts markdown text to RTF format for display in RichTextBox.
    /// Supports: **bold**, *italic*, `inline code`, ```code blocks```, bullet lists (- or *), and # headers.
    /// </summary>
    internal static class MarkdownExtensions
    {
        // Font indices in the font table
        private const int FontDefault = 0;
        private const int FontMono = 1;

        // Color indices in the color table
        private const int ColorDefault = 1;
        private const int ColorCode = 2;
        private const int ColorHeader = 3;

        /// <summary>
        /// Converts markdown text to RTF format.
        /// </summary>
        /// <param name="markdown">The markdown text to convert.</param>
        /// <param name="foreColor">The default foreground color.</param>
        /// <param name="backColor">The background color (used for code blocks styling hint).</param>
        /// <param name="fontSize">The base font size in points.</param>
        /// <returns>RTF formatted string.</returns>
        public static string ConvertMarkdownToRtf(this string markdown, Color foreColor, Color backColor, float fontSize = 9)
        {
            var baseFontSize = (int)System.Math.Round(fontSize);
            return ConvertMarkdownToRtf(markdown, foreColor, backColor, baseFontSize, null, FontStyle.Regular);
        }

        public static string ConvertMarkdownToRtf(this string markdown, Color foreColor, Color backColor, Font baseFont)
        {
            if (baseFont == null)
            {
                throw new ArgumentNullException(nameof(baseFont));
            }

            // Use point size from the Font; RTF \fs is in half-points
            var baseFontSize = (int)System.Math.Round(baseFont.Size);

            return ConvertMarkdownToRtf(
                markdown,
                foreColor,
                backColor,
                baseFontSize,
                baseFont.Name,
                baseFont.Style);
        }

        private static string ConvertMarkdownToRtf(
            string markdown,
            Color foreColor,
            Color backColor,
            int fontSize,
            string fontName,
            FontStyle fontStyle)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return BuildRtfDocument("", foreColor, backColor, fontSize, fontName, fontStyle);
            }

            // Normalize line endings to UNIX style (LF)
            markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

            var codeBlocks = new List<string>();
            // Extract and replace code blocks with placeholders
            markdown = ExtractCodeBlocks(markdown, codeBlocks);

            var result = new StringBuilder();

            // Split into lines for per-line processing
            var lines = markdown.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                line = line.TrimEnd('\r', '\n');

                var processedLine = ProcessLine(line, codeBlocks, fontSize);
                result.Append(processedLine);

                // Add paragraph break except after the last line
                if (i < lines.Length - 1)
                {
                    result.Append("\\par\n");
                }
            }

            return BuildRtfDocument(result.ToString(), foreColor, backColor, fontSize, fontName, fontStyle);
        }

        public static bool IsMarkdown(this string text)
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
            // - Headers: # Header
            // - Bullet lists: - item or * item
            var patterns = new[]
            {
                @"\*\*(.+?)\*\*",          // Bold
                @"(?<!\*)\*(.+?)\*(?!\*)", // Italic
                @"`([^`]*)`",              // Inline code
                @"```[\s\S]*?```",         // Code blocks
                @"^#{1,6}\s+.+$",          // Headers
                @"^(\s*)([-*])\s+.+$"      // Bullet lists
            };
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(text, pattern, RegexOptions.Multiline))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ExtractCodeBlocks(string markdown, List<string> codeBlocks)
        {
            // Match code blocks: ```[language]\n...\n``` or ```...```
            var codeBlockPattern = new Regex(@"```(?:\w*\n)?([\s\S]*?)```", RegexOptions.Multiline);

            return codeBlockPattern.Replace(markdown, match =>
            {
                var codeContent = match.Groups[1].Value.TrimEnd('\n', '\r');
                var index = codeBlocks.Count;
                codeBlocks.Add(codeContent);
                // Use a unique placeholder that won't appear in normal text
                return $"__CODEBLOCK_{index}_PLACEHOLDER__";
            });
        }

        private static string ProcessLine(string line, List<string> codeBlocks, int fontSize)
        {
            // Check for code block placeholder - must be the entire (trimmed) line
            var trimmed = line.Trim();
            var codeBlockMatch = Regex.Match(trimmed, @"^__CODEBLOCK_(\d+)_PLACEHOLDER__$");
            if (codeBlockMatch.Success)
            {
                var index = int.Parse(codeBlockMatch.Groups[1].Value);
                var codeContent = codeBlocks[index];
                return FormatCodeBlock(codeContent);
            }

            // Horizontal rule: line of exactly three or more dashes
            if (Regex.IsMatch(trimmed, @"^-{3,}$"))
            {
                return FormatHorizontalRule();
            }

            // Check for headers (# Header, ## Header, etc.)
            var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (headerMatch.Success)
            {
                var level = headerMatch.Groups[1].Value.Length;
                var headerText = headerMatch.Groups[2].Value;
                return FormatHeader(headerText, level, fontSize);
            }

            // Ordered list items: 1. Item text (any amount of leading spaces)
            var orderedMatch = Regex.Match(line, @"^(\s*)(\d+)\.\s+(.+)$");
            if (orderedMatch.Success)
            {
                var indentText = orderedMatch.Groups[1].Value;
                var leadingUnits = GetIndentWidth(indentText);
                var number = orderedMatch.Groups[2].Value;
                var content = orderedMatch.Groups[3].Value;
                return FormatOrderedListItem(number, content, leadingUnits);
            }

            // Unordered list items: -, * or + followed by text
            var unorderedMatch = Regex.Match(line, @"^(\s*)([-*+])\s+(.+)$");
            if (unorderedMatch.Success)
            {
                var indentText = unorderedMatch.Groups[1].Value;
                var leadingUnits = GetIndentWidth(indentText);
                var content = unorderedMatch.Groups[3].Value;
                return FormatUnorderedListItem(content, leadingUnits);
            }

            // Regular line - process inline formatting
            return ProcessInlineFormatting(line);
        }

        private static string ProcessInlineFormatting(string text)
        {
            // Escape RTF special characters first
            text = EscapeRtf(text);

            // Process inline code first (to protect from bold/italic processing)
            text = ProcessInlineCode(text);

            // Process bold (**text**) - must be before italic
            text = ProcessBold(text);

            // Process italic (*text*) - careful not to match bullet markers
            text = ProcessItalic(text);

            return text;
        }

        private static string ProcessInlineCode(string text)
        {
            // Match `code` (including empty) but not inside already processed markers
            var pattern = new Regex(@"`([^`]*)`");
            return pattern.Replace(text, match =>
            {
                var code = match.Groups[1].Value;
                // Use monospace font and code color, but do not override base font elsewhere
                return $"{{\\f{FontMono}\\cf{ColorCode} {code}}}";
            });
        }

        private static string ProcessBold(string text)
        {
            // Match **bold text**
            var pattern = new Regex(@"\*\*(.+?)\*\*");
            return pattern.Replace(text, match =>
            {
                var boldText = match.Groups[1].Value;
                // Use braces to explicitly scope the bold formatting
                return $"{{\\b {boldText}}}";
            });
        }

        private static string ProcessItalic(string text)
        {
            // Match *italic text* or _italic text_ but not ** or __ (bold markers)
            // Use negative lookbehind and lookahead to avoid matching bold markers
            var pattern = new Regex("(?<![*_])[*_](.+?)[*_](?![*_])");
            return pattern.Replace(text, match =>
            {
                var italicText = match.Groups[1].Value;
                // Use braces to implicitly scope the italic formatting
                return $"{{\\i {italicText}}}";
            });
        }

        private static string FormatCodeBlock(string code)
        {
            // Escape RTF special characters in code
            code = EscapeRtf(code);

            // Replace newlines with RTF line breaks, preserving indentation
            var lines = code.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var indentMatch = Regex.Match(line, @"^(\s*)");
                var indent = indentMatch.Success ? indentMatch.Groups[1].Value : string.Empty;
                var escapedIndent = indent.Replace(" ", " ").Replace("\t", "    ");
                lines[i] = escapedIndent + line.Substring(indent.Length);
            }
            code = string.Join("\\line\n", lines);

            // Wrap the whole code block in a slightly indented paragraph so the block stands out
            // Use monospace font and code color for the text
            return "\\pard\\li240" +            // 1/6 inch left indent for the block
                   $" {{\\f{FontMono}\\cf{ColorCode} {code}}}" +
                   "\\par\\pard";              // end block and reset paragraph settings
        }

        private static string FormatHeader(string text, int level, int baseFontSize)
        {
            text = ProcessInlineFormatting(text);

            double multiplier;
            switch (level)
            {
                case 1:
                    multiplier = 2.0;   // biggest
                    break;

                case 2:
                    multiplier = 1.6;
                    break;

                case 3:
                    multiplier = 1.3;
                    break;

                case 4:
                    multiplier = 1.1;   // slightly larger than body
                    break;

                default:
                    multiplier = 1.0;   // h5/h6 same as body, but bold + color
                    break;
            }

            var headerSize = (int)(baseFontSize * multiplier * 2); // RTF uses half-points
            return $"{{\\fs{headerSize}\\b\\cf{ColorHeader} {text}}}";
        }

        // Legacy bullet formatter not used anymore; list rendering now goes via
        // FormatUnorderedListItem and FormatOrderedListItem.
        private static string FormatBullet(string content, int level)
        {
            return FormatUnorderedListItem(content, level);
        }

        private static string FormatUnorderedListItem(string content, int leadingUnits)
        {
            content = ProcessInlineFormatting(content);

            const int baseIndentTwips = 360; // whole list block shifted in
            const int twipsPerUnit = 180;    // ~0.125" per unit

            var leftIndent = baseIndentTwips + (leadingUnits * twipsPerUnit);
            // Hanging indent, but no trailing \par – outer loop adds it
            return string.Format("\\pard\\li{0}\\fi-180 \\u8226 ?  {1}", leftIndent, content);
        }

        private static string FormatOrderedListItem(string number, string content, int leadingUnits)
        {
            content = ProcessInlineFormatting(content);

            const int baseIndentTwips = 360;
            const int twipsPerUnit = 180;

            var leftIndent = baseIndentTwips + (leadingUnits * twipsPerUnit);
            // Same pattern for numbered items
            return string.Format("\\pard\\li{0}\\fi-180 {1}.  {2}", leftIndent, number, content);
        }

        private static string FormatHorizontalRule()
        {
            // Simple horizontal rule using ASCII dashes so it works with plain fonts

            // RichTextBox doesn't support true <hr>, so draw a text-based line
            // won't work with this unicode escapes... :/
            //return "\\pard\\qc\\u8212 ?\\u8212 ?\\u8212 ?\\pard";

            // Center it using paragraph alignment
            return "\\pard\\qc------------------------------\\pard";
        }

        private static string EscapeRtf(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var result = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                switch (c)
                {
                    case '\\':
                        result.Append("\\\\");
                        break;

                    case '{':
                        result.Append("\\{");
                        break;

                    case '}':
                        result.Append("\\}");
                        break;

                    default:
                        if (c > 127)
                        {
                            // Unicode character - use RTF unicode escape
                            result.Append($"\\u{(int)c}?");
                        }
                        else
                        {
                            // ASCII characters including '"' are written as-is
                            result.Append(c);
                        }
                        break;
                }
            }
            return result.ToString();
        }

        private static string EscapeFontName(string name)
        {
            // Minimal: escape backslashes/braces for safety
            return name.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");
        }

        private static string BuildRtfDocument(string content, Color foreColor, Color backColor, int fontSize, string fontName, FontStyle fontStyle)
        {
            var backBrightness = backColor.R * 0.299 + backColor.G * 0.587 + backColor.B * 0.114;
            var codeForeColor = backBrightness < 128 ? Color.White : Color.Black;
            var headerColor = foreColor;

            var rtf = new StringBuilder();
            rtf.Append("{\\rtf1\\ansi\\deff0");

            // Font table: f0 = caller’s base font (if provided), f1 = monospace
            rtf.Append("{\\fonttbl");

            if (!string.IsNullOrEmpty(fontName))
            {
                rtf.Append($"{{\\f0\\fnil {EscapeFontName(fontName)};}}");
            }
            else
            {
                rtf.Append("{\\f0;}");
            }

            rtf.Append("{\\f1\\fmodern Consolas;}");
            rtf.Append("}");

            // Color table
            rtf.Append("{\\colortbl;");
            rtf.Append($"\\red{foreColor.R}\\green{foreColor.G}\\blue{foreColor.B};");
            rtf.Append($"\\red{codeForeColor.R}\\green{codeForeColor.G}\\blue{codeForeColor.B};");
            rtf.Append($"\\red{headerColor.R}\\green{headerColor.G}\\blue{headerColor.B};");
            rtf.Append("}");

            rtf.Append("\\viewkind4\\uc1");

            // Base font size from caller; RTF uses half-points
            var baseFs = fontSize > 0 ? fontSize * 2 : 0;
            if (baseFs > 0)
            {
                rtf.Append($"\\pard\\f{FontDefault}\\fs{baseFs}\\cf{ColorDefault} ");
            }
            else
            {
                rtf.Append($"\\pard\\f{FontDefault}\\cf{ColorDefault} ");
            }

            // Could also honor bold/italic from FontStyle if you want:
            // if ((fontStyle & FontStyle.Bold) != 0) rtf.Append("\\b ");
            // if ((fontStyle & FontStyle.Italic) != 0) rtf.Append("\\i ");

            rtf.Append(content);
            rtf.Append("}");

            return rtf.ToString();
        }

        private static int GetIndentWidth(string indent)
        {
            // Treat a tab as 4 spaces; each space counts as 1 unit
            var width = 0;
            foreach (var ch in indent)
            {
                if (ch == '\t')
                {
                    width += 4;
                }
                else if (ch == ' ')
                {
                    width += 1;
                }
            }
            return width;
        }
    }
}