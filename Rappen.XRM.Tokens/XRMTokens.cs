using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;

namespace Rappen.XRM.Tokens
{
    public static class XRMTokens
    {
        #region Public extensions methods

        public static string Tokens(this Entity entity, IOrganizationService service, string text) => Tokens(entity, new GenericBag(service), text);

        public static string Tokens(this Entity entity, IBag bag, string text) => Tokens(entity, bag, text, 0, string.Empty);

        public static string Tokens(this Entity entity, IBag bag, string text, int sequence) => Tokens(entity, bag, text, sequence, string.Empty);

        public static string Tokens(this Entity entity, IBag bag, string text, int sequence, string scope) => Tokens(entity, bag, text, sequence, scope, false);

        public static string Tokens(this Entity entity, IBag bag, string text, int sequence, string scope, bool supressinvalidattributepaths) => Tokens(entity, bag, text, sequence, scope, null, supressinvalidattributepaths);

        #endregion Public extensions methods

        #region Private static properties

        private const string prevent_recursion_start = "~~PRERECCURLYSTART~~";
        private const string prevent_recursion_end = "~~PRERECCURLYEND~~";
        private const string special_chars_curly_start = "~~SPECIALCHARCURLYSTART~~";
        private const string special_chars_curly_end = "~~SPECIALCHARCURLYEND~~";

        #endregion Private static properties

        #region Private static methods

        private static string Tokens(Entity entity, IBag bag, string text, int sequence, string scope, Dictionary<string, string> replacepatterns, bool supressinvalidattributepaths)
        {
            bag.Logger.StartSection("XRM Tokens " + scope);
            if (text == null)
            {
                text = string.Empty;
            }

            // Half-smart solution to handle system chars for { and }, without breaking the tokens
            text = text
                .Replace("<system|char|{>", special_chars_curly_start)
                .Replace("<system|char|}>", special_chars_curly_end);

            // Half-stupid solution to handle tags that have been encoded...
            if (text.Contains("&lt;expand|") || text.Contains("&lt;iif|") || text.Contains("&lt;system|"))
            {
                text = text.Replace("&lt;", "<").Replace("&gt;", ">");
            }

            // Looking for "all" or tags with a specific entity scope?
            var starttag = string.IsNullOrEmpty(scope) ? "" : "" + scope + ":";

            var token = text.GetNextToken(starttag);
            while (!string.IsNullOrWhiteSpace(token))
            {
                bag.Logger.Log($"Found token: {token}");
                if (token.StartsWith(starttag + "PowerFx|", StringComparison.Ordinal))
                {
                    text = entity.ReplacePowerFx(bag, text, sequence, scope, supressinvalidattributepaths, token);
                }
                else if (token.StartsWith(starttag + "expand|", StringComparison.Ordinal))
                {
                    text = entity.Expand(bag, text, token);
                }
                else if (token.StartsWith(starttag + "iif|", StringComparison.Ordinal))
                {
                    text = entity.EvaluateIif(bag, text, scope, token);
                }
                else if (token.ToLowerInvariant().StartsWith(starttag + "system|", StringComparison.Ordinal))
                {
                    text = Systems.ReplaceSystem(bag, text, token);
                }
                else if (token.ToLowerInvariant().StartsWith(starttag + "random|", StringComparison.Ordinal))
                {
                    text = Randoms.ReplaceRandom(bag, text, token);
                }
                else
                {
                    text = entity.ReplaceEntityTokens(bag, text, scope, replacepatterns, supressinvalidattributepaths, token);
                }

                token = text.GetNextToken(starttag);
            }

            // ReReplace curly things in the result to not rerun token replaces
            text = text.Replace(prevent_recursion_start, "{").Replace(prevent_recursion_end, "}");

            // Half-smart replacing those { and } handling just before returning the result
            text = text.Replace(special_chars_curly_start, "{").Replace(special_chars_curly_end, "}");

            if (sequence > 0)
            {
                text = text.ReplaceSequence(sequence);
            }

            bag.Logger.EndSection();
            return text;
        }

        internal static string ReplaceFirstOnly(this string theString, string oldValue, string newValue)
        {
            if (!theString.Contains(oldValue))
            {
                return theString;
            }
            var pos = theString.IndexOf(oldValue);
            var newString = theString.Substring(0, pos);
            newString += newValue;
            newString += theString.Substring(pos + oldValue.Length);
            return newString;
        }

        #endregion Private static methods
    }
}