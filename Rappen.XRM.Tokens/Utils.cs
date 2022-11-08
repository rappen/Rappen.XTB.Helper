using System;

namespace Rappen.XRM.Tokens
{
    internal static class Utils
    {
        internal static string GetSeparatedPart(this string source, string separator, int partno)
        {
            int tagcount = 0;
            int pos = 0;
            int separatorcount = 0;
            int startpos = -1;
            while (separatorcount < partno && pos < source.Length)
            {
                if (startpos == -1 && separatorcount == partno - 1 && tagcount == 0)
                {
                    startpos = pos;
                }

                char character = source[pos];
                if (character == '>' || character == '}')
                {
                    tagcount--;
                }
                else if (character == '<' || character == '{')
                {
                    tagcount++;
                }

                if (tagcount == 0 && source.Substring(pos).StartsWith(separator, StringComparison.Ordinal))
                {
                    separatorcount++;
                }

                pos++;
            }
            int length = pos == source.Length ? pos - startpos : pos - startpos - 1;
            string result = "";
            if (startpos >= 0 && startpos + length <= source.Length)
            {
                result = source.Substring(startpos, length);
            }

            if (result.EndsWith(separator, StringComparison.Ordinal))
            {   // Special case when the complete token ends with the separator
                result = result.Substring(0, result.Length - separator.Length);
            }
            return result;
        }
    }
}