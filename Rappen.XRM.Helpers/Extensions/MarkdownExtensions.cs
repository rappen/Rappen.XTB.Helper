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
        public static string ConvertMarkdownToRtf(this string markdown, Color foreColor, Color backColor, int fontSize = 9)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return BuildRtfDocument("", foreColor, backColor, fontSize);
            }

            // Normalize line endings
            markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

            // Extract code blocks first to protect them from other processing
            var codeBlocks = new List<string>();
            markdown = ExtractCodeBlocks(markdown, codeBlocks);

            // Process the markdown line by line for structure (headers, bullets)
            var lines = markdown.Split('\n');
            var result = new StringBuilder();

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var processedLine = ProcessLine(line, codeBlocks, fontSize);
                result.Append(processedLine);

                // Add paragraph break except for last line
                if (i < lines.Length - 1)
                {
                    result.Append("\\par\n");
                }
            }

            return BuildRtfDocument(result.ToString(), foreColor, backColor, fontSize);
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
            // Check for code block placeholder
            var codeBlockMatch = Regex.Match(line, @"__CODEBLOCK_(\d+)_PLACEHOLDER__");
            if (codeBlockMatch.Success)
            {
                var index = int.Parse(codeBlockMatch.Groups[1].Value);
                var codeContent = codeBlocks[index];
                return FormatCodeBlock(codeContent);
            }

            // Horizontal rule: line of exactly three or more dashes
            if (Regex.IsMatch(line.Trim(), @"^-{3,}$"))
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

            // Check for bullet lists (- item or * item, but not **bold**)
            var bulletMatch = Regex.Match(line, @"^(\s*)([-])\s+(.+)$|^(\s*)(\*)\s+(?!\*)(.+)$");
            if (bulletMatch.Success)
            {
                // For '-' bullets, use groups 1 and 3; for '*' bullets, use groups 4 and 6
                var indent = bulletMatch.Groups[1].Success ? bulletMatch.Groups[1].Value : bulletMatch.Groups[4].Value;
                var content = bulletMatch.Groups[3].Success ? bulletMatch.Groups[3].Value : bulletMatch.Groups[6].Value;
                return FormatBullet(content, indent.Length);
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
                // Use monospace font and code color
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
            // Match *italic text* but not ** (bold markers)
            // Use negative lookbehind and lookahead to avoid matching bold markers
            var pattern = new Regex(@"(?<!\*)\*(.+?)\*(?!\*)");
            return pattern.Replace(text, match =>
            {
                var italicText = match.Groups[1].Value;
                // Use braces to explicitly scope the italic formatting
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

            // Stronger size differences by level
            double multiplier;
            switch (level)
            {
                case 1:
                    multiplier = 2.0;   // H1: 2x
                    break;

                case 2:
                    multiplier = 1.6;   // H2: 1.6x
                    break;

                case 3:
                    multiplier = 1.3;   // H3: 1.3x
                    break;

                default:
                    multiplier = 1.15;  // H4+ : slightly larger than body
                    break;
            }

            var headerSize = (int)(baseFontSize * multiplier * 2); // RTF uses half-points

            return $"{{\\fs{headerSize}\\b\\cf{ColorHeader} {text}}}";
        }

        private static string FormatBullet(string content, int indentLevel)
        {
            // Process inline formatting within the bullet content
            content = ProcessInlineFormatting(content);

            // Calculate indent in twips (1 inch = 1440 twips, use 360 twips per indent level)
            var indent = 360 + (indentLevel * 180);

            // Place bullet at left margin and text just to the right, minimal gap
            var textStart = indent + 80;   // small gap (~0.05 inch)

            return $"\\pard\\li{textStart}\\tx{textStart}\\fi-80\\bullet\\tab {content}\\pard";
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

        private static string BuildRtfDocument(string content, Color foreColor, Color backColor, int fontSize)
        {
            // Decide code foreground: black on light background, white on dark background
            var backBrightness = backColor.R * 0.299 + backColor.G * 0.587 + backColor.B * 0.114;
            var codeForeColor = backBrightness < 128 ? Color.White : Color.Black;

            // Header color - same as foreground but could be customized
            var headerColor = foreColor;

            var rtf = new StringBuilder();

            // RTF header
            rtf.Append("{\\rtf1\\ansi\\deff0");

            // Font table: f0 = default UI font (Calibri), f1 = monospace for code
            rtf.Append("{\\fonttbl");
            rtf.Append("{\\f0\\fswiss\\fcharset0 Calibri;}");
            rtf.Append("{\\f1\\fmodern\\fcharset0 Consolas;}");
            rtf.Append("}");

            // Color table
            rtf.Append("{\\colortbl;");
            rtf.Append($"\\red{foreColor.R}\\green{foreColor.G}\\blue{foreColor.B};");          // cf1 - default text
            rtf.Append($"\\red{codeForeColor.R}\\green{codeForeColor.G}\\blue{codeForeColor.B};"); // cf2 - code text (black/white vs background)
            rtf.Append($"\\red{headerColor.R}\\green{headerColor.G}\\blue{headerColor.B};");     // cf3 - headers
            rtf.Append("}");

            // Document settings
            rtf.Append("\\viewkind4\\uc1");

            // Initialize paragraph and set default font, size, and color
            var rtfFontSize = fontSize * 2; // RTF uses half-points
            rtf.Append($"\\pard\\f{FontDefault}\\fs{rtfFontSize}\\cf{ColorDefault} ");

            // Content
            rtf.Append(content);

            // Close RTF document
            rtf.Append("}");

            return rtf.ToString();
        }
    }
}