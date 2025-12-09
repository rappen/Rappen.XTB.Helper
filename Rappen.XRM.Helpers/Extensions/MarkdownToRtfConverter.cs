using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace Rappen.XRM.Helpers.Extensions
{
    /// <summary>
    /// Converts markdown text to RTF format for display in RichTextBox.
    /// Supports: **bold**, *italic*, `inline code`, ```code blocks```, bullet lists (- or *), and # headers.
    /// </summary>
    internal static class MarkdownToRtfConverter
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
        public static string Convert(string markdown, Color foreColor, Color backColor, int fontSize = 9)
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

            for (int i = 0; i < lines.Length; i++)
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

        private static string ExtractCodeBlocks(string markdown, List<string> codeBlocks)
        {
            // Match code blocks: ```[language]\n...\n``` or ```...```
            var codeBlockPattern = new Regex(@"```(?:\w*\n)?([\s\S]*?)```", RegexOptions.Multiline);

            return codeBlockPattern.Replace(markdown, match =>
            {
                var codeContent = match.Groups[1].Value.Trim();
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

            // Check for headers (# Header, ## Header, etc.)
            var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (headerMatch.Success)
            {
                var level = headerMatch.Groups[1].Value.Length;
                var headerText = headerMatch.Groups[2].Value;
                return FormatHeader(headerText, level, fontSize);
            }

            // Check for bullet lists (- item or * item, but not **bold**)
            var bulletMatch = Regex.Match(line, @"^(\s*)([-*])\s+(?!\*)(.+)$");
            if (bulletMatch.Success)
            {
                var indent = bulletMatch.Groups[1].Value;
                var content = bulletMatch.Groups[3].Value;
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
            // Match `code` but not inside already processed markers
            var pattern = new Regex(@"`([^`]+)`");
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
            var pattern = new Regex(@"(?<!\*)\*([^*]+)\*(?!\*)");
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

            // Replace newlines with RTF line breaks
            code = code.Replace("\n", "\\line\n");

            // Use monospace font and code color for the entire block
            return $"{{\\f{FontMono}\\cf{ColorCode} {code}}}";
        }

        private static string FormatHeader(string text, int level, int baseFontSize)
        {
            // Process inline formatting within the header
            text = ProcessInlineFormatting(text);

            // Calculate header font size (larger for smaller level numbers)
            // Level 1 = 1.5x, Level 2 = 1.3x, Level 3+ = 1.15x
            double multiplier = level == 1 ? 1.5 : level == 2 ? 1.3 : 1.15;
            int headerSize = (int)(baseFontSize * multiplier * 2); // RTF uses half-points

            // Format as bold with larger font and header color, using braces for scoping
            return $"{{\\fs{headerSize}\\b\\cf{ColorHeader} {text}}}";
        }

        private static string FormatBullet(string content, int indentLevel)
        {
            // Process inline formatting within the bullet content
            content = ProcessInlineFormatting(content);

            // Calculate indent in twips (1 inch = 1440 twips, use 360 twips per indent level)
            int indent = 360 + (indentLevel * 180);

            // Use bullet character (Unicode 2022) with indentation
            return $"\\li{indent}\\bullet  {content}\\li0";
        }

        private static string EscapeRtf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = new StringBuilder(text.Length);
            foreach (char c in text)
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
                            result.Append(c);
                        }
                        break;
                }
            }
            return result.ToString();
        }

        private static string BuildRtfDocument(string content, Color foreColor, Color backColor, int fontSize)
        {
            // Calculate a code color - slightly different from foreground
            var codeColor = BlendColors(foreColor, Color.Gray, 0.3);

            // Header color - same as foreground but could be customized
            var headerColor = foreColor;

            var rtf = new StringBuilder();

            // RTF header
            rtf.Append("{\\rtf1\\ansi\\deff0");

            // Font table: f0 = default UI font, f1 = monospace for code
            rtf.Append("{\\fonttbl");
            rtf.Append("{\\f0\\fswiss\\fcharset0 Segoe UI;}");
            rtf.Append("{\\f1\\fmodern\\fcharset0 Consolas;}");
            rtf.Append("}");

            // Color table
            rtf.Append("{\\colortbl;");
            rtf.Append($"\\red{foreColor.R}\\green{foreColor.G}\\blue{foreColor.B};");     // cf1 - default
            rtf.Append($"\\red{codeColor.R}\\green{codeColor.G}\\blue{codeColor.B};");     // cf2 - code
            rtf.Append($"\\red{headerColor.R}\\green{headerColor.G}\\blue{headerColor.B};"); // cf3 - header
            rtf.Append("}");

            // Document settings
            rtf.Append("\\viewkind4\\uc1");

            // Initialize paragraph and set default font, size, and color
            int rtfFontSize = fontSize * 2; // RTF uses half-points
            rtf.Append($"\\pard\\f{FontDefault}\\fs{rtfFontSize}\\cf{ColorDefault} ");

            // Content
            rtf.Append(content);

            // Close RTF document
            rtf.Append("}");

            return rtf.ToString();
        }

        private static Color BlendColors(Color color1, Color color2, double amount)
        {
            int r = (int)(color1.R + (color2.R - color1.R) * amount);
            int g = (int)(color1.G + (color2.G - color1.G) * amount);
            int b = (int)(color1.B + (color2.B - color1.B) * amount);
            return Color.FromArgb(r, g, b);
        }
    }
}
