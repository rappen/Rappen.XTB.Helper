using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Text;

namespace Rappen.AI.WinForm
{
    public static class MarkdownToRtfConverter
    {
        /// <summary>
        /// Main entry point: converts Markdown text to RTF.
        /// </summary>
        /// <param name="markdown">A string containing Markdown markup.</param>
        /// <returns>An RTF-formatted string.</returns>
        public static string Convert(string markdown)
        {
            // 1) Parse Markdown into a syntax tree
            var pipeline = new MarkdownPipelineBuilder().Build();
            var document = Markdig.Markdown.Parse(markdown, pipeline);

            // 2) Start building the RTF string
            var rtfBuilder = new StringBuilder();

            // Basic RTF header
            rtfBuilder.AppendLine(@"{\rtf1\ansi\deff0");
            //rtfBuilder.AppendLine(@"{\fonttbl{\f0 Arial;}}");

            // You can add more RTF preamble here (font tables, color tables, etc.)

            // 3) Walk the document blocks
            foreach (var block in document)
            {
                switch (block)
                {
                    case HeadingBlock headingBlock:
                        ConvertHeadingBlock(rtfBuilder, headingBlock);
                        break;

                    case ParagraphBlock paragraphBlock:
                        ConvertParagraphBlock(rtfBuilder, paragraphBlock);
                        break;

                    case ListBlock listBlock:
                        ConvertListBlock(rtfBuilder, listBlock);
                        break;

                    default:
                        // Unhandled block type; extend as needed
                        break;
                }
            }

            // Close the RTF document
            rtfBuilder.AppendLine("}");

            return rtfBuilder.ToString();
        }

        /// <summary>
        /// Handles Markdown heading blocks (e.g., # Heading1, ## Heading2, etc.).
        /// </summary>
        private static void ConvertHeadingBlock(StringBuilder rtf, HeadingBlock headingBlock)
        {
            // Map heading level to font size (completely arbitrary example)
            // RTF uses \fsN in half-points (e.g., \fs32 => 16pt)
            int[] headingSizes = { 30, 28, 26, 24, 22, 20 };
            int headingLevel = headingBlock.Level; // 1-based

            // Get a font size for the heading level, clamp if needed
            int fontSize = headingSizes[Math.Min(headingLevel, headingSizes.Length) - 1];

            rtf.Append($@"\pard\sa180\fs{fontSize} \b ");
            // Heading text:
            ConvertInline(rtf, headingBlock.Inline);
            // End bold, new line
            rtf.AppendLine(@"\b0\par");
        }

        /// <summary>
        /// Handles normal paragraphs.
        /// </summary>
        private static void ConvertParagraphBlock(StringBuilder rtf, ParagraphBlock paragraphBlock)
        {
            rtf.Append(@"\pard\sa180\fs20 ");
            // Convert inlines inside this paragraph
            ConvertInline(rtf, paragraphBlock.Inline);

            // End paragraph
            rtf.AppendLine(@"\par");
        }

        /// <summary>
        /// Handles Markdown lists (ordered or unordered).
        /// </summary>
        private static void ConvertListBlock(StringBuilder rtf, ListBlock listBlock)
        {
            bool isOrdered = listBlock.IsOrdered;

            foreach (var item in listBlock)
            {
                // Each list item is itself a ListItemBlock containing sub-blocks.
                if (item is ListItemBlock listItemBlock)
                {
                    // Start the bullet or number
                    string prefix = isOrdered
                        ? $"{listItemBlock.Order}. "   // e.g., "1. ", "2. ", etc.
                        : @"\bullet ";                // or just a bullet symbol, e.g. \bullet

                    rtf.Append(@"\pard\sa100\fs20 ");
                    rtf.Append(prefix);
                    //rtf.Append(" ");

                    // Convert each sub-block inside this list item
                    foreach (var subBlock in listItemBlock)
                    {
                        switch (subBlock)
                        {
                            case ParagraphBlock subParagraph:
                                ConvertInline(rtf, subParagraph.Inline);
                                break;

                            // Extend if you have nested lists, code, etc.
                            default:
                                break;
                        }
                    }

                    // End list item
                    rtf.AppendLine(@"\par");
                }
            }
        }

        /// <summary>
        /// Recursively handles inlines (bold, italic, underline, etc.) in a Markdig Inline container.
        /// </summary>
        private static void ConvertInline(StringBuilder rtf, ContainerInline containerInline, string prefix = "")
        {
            foreach (var inline in containerInline)
            {
                switch (inline)
                {
                    case EmphasisInline emphasisInline:
                        HandleEmphasis(rtf, emphasisInline);
                        break;

                    case LineBreakInline lineBreakInline:
                        // Soft line break or hard line break?
                        // For simplicity, just do a line break.
                        rtf.Append(@"\line ");
                        break;

                    case CodeInline codeInline:
                        // For code inline, you might do a monospace font or something else
                        rtf.Append(@"\f1 "); // e.g., a monospace font
                        rtf.Append(EscapeRtf(codeInline.Content));
                        rtf.Append(@"\f0 ");
                        break;

                    case HtmlInline htmlInline:
                        // Could try to interpret inline HTML, or just skip/escape
                        rtf.Append(EscapeRtf(htmlInline.Tag));
                        break;

                    case LinkInline linkInline:
                        // A link might show as underlined text + possibly a hidden URL
                        // This is just a simplistic representation
                        rtf.Append(@"\ul ");
                        rtf.Append(EscapeRtf(prefix));
                        rtf.Append(EscapeRtf(linkInline.Title ?? linkInline.Url));
                        rtf.Append(@"\ulnone ");
                        break;

                    case LiteralInline literalInline:
                        rtf.Append(EscapeRtf(literalInline.Content.ToString()));
                        break;

                    default:
                        // Not handled; no-op
                        break;
                }
            }
        }

        /// <summary>
        /// Handles emphasis inlines (e.g., *italic*, **bold**, ***bold+italic***, etc.).
        /// We also interpret underscores as underline in this example.
        /// </summary>
        private static void HandleEmphasis(StringBuilder rtf, EmphasisInline emphasisInline)
        {
            // Markdig uses DelimiterChar = '*' for bold/italic, '_' is also possible
            bool isItalic = (emphasisInline.DelimiterChar == '*' && emphasisInline.DelimiterCount == 1)
                            || (emphasisInline.DelimiterChar == '_' && emphasisInline.DelimiterCount == 1);

            bool isBold = (emphasisInline.DelimiterChar == '*' && emphasisInline.DelimiterCount == 2);
            bool isUnderline = (emphasisInline.DelimiterChar == '_' && emphasisInline.DelimiterCount == 2);

            // For triple *** or ___, Markdig generally splits it into nested emphasis inlines
            // but you could handle combined styles if desired.

            // Start tags
            if (isBold) rtf.Append(@"\b ");
            if (isItalic) rtf.Append(@"\i ");
            if (isUnderline) rtf.Append(@"\ul ");

            // Recursively process the content inside the emphasis
            ConvertInline(rtf, emphasisInline);

            // End tags (reverse order)
            if (isUnderline) rtf.Append(@" \ulnone ");
            if (isItalic) rtf.Append(@" \i0 ");
            if (isBold) rtf.Append(@" \b0 ");
        }

        /// <summary>
        /// RTF is sensitive to certain special characters. Escape them here.
        /// </summary>
        private static string EscapeRtf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Replace backslash, curly braces
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;

                    case '{':
                        sb.Append("\\{");
                        break;

                    case '}':
                        sb.Append("\\}");
                        break;
                    // Convert newline to \line or \par if desired,
                    // but let's do it in the inline logic instead
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}