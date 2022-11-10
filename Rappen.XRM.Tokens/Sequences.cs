using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Extensions;

namespace Rappen.XRM.Tokens
{
    internal static class Sequences
    {
        internal static string ReplaceSequence(this string text, int sequence)
        {
            var num = text.GetFirstEnclosedPart("[", "#", "]", string.Empty);
            while (!string.IsNullOrWhiteSpace(num))
            {
                var startnostr = "0" + num.GetSeparatedPart("|", 2);
                if (!int.TryParse(startnostr, out int startno))
                {
                    throw new InvalidPluginExecutionException($"Sequence start value invalid: {startnostr}");
                }
                if (startno > 0)
                {
                    startno--;
                }
                var format = num.GetSeparatedPart("|", 3);
                var currentvalue = startno + sequence;
                text = text.Replace("[" + num + "]", currentvalue.ToString(format));
                num = text.GetFirstEnclosedPart("[", "#", "]", string.Empty);
            }
            return text;
        }
    }
}