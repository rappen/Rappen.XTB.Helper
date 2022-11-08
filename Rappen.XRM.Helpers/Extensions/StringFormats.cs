using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class StringFormats
    {
        private static List<string> extraFormatTags = new List<string>() { "MaxLen", "Pad", "Left", "Right", "Trim", "TrimStart", "TrimEnd", "SubStr", "Replace", "Math", "Upper", "Lower" };

        #region Public Static Extensions Methods

        public static string GetSeparatedPart(this string source, string separator, int partno)
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

        public static string GetFirstEnclosedPart(this string source, string starttag, string keyword, string endtag, string scope)
        {
            var result = GetFirstEnclosedPartStartString(source, starttag, keyword, endtag, scope);
            if (string.IsNullOrEmpty(result) || !source.Contains(endtag))
            {   // Felaktiga start/end eller keyword
                return "";
            }
            if (string.IsNullOrEmpty(scope)
                && ComparePositions(source, "{", ":") < 0   // Startkrull före kolon
                && ComparePositions(source, "}", ":") > 0   // Slutkrull före kolon
                && ComparePositions(source, ":", "<") < 0   // Kolon före starttag
                && ComparePositions(source, ":", "|") < 0)  // Kolon före format-pipe
            {   // Det finns ett kolon som avser namespace, men inget namespace var angivet
                return "";
            }
            int tagcount = 1;
            int pos = 0;
            while (pos < result.Length && tagcount > 0)
            {
                if (result.Substring(pos).StartsWith(endtag, StringComparison.Ordinal))
                {
                    tagcount--;
                }
                else if (result.Substring(pos).StartsWith(starttag, StringComparison.Ordinal))
                {
                    tagcount++;
                }

                pos++;
            }
            if (tagcount > 0)
            {
                throw new InvalidOperationException("GetFirstEnclosedPart: Missing end tag: " + endtag);
            }

            result = result.Substring(0, pos - 1);
            return result;
        }

        public static int ComparePositions(this string source, string item1, string item2)
        {
            return (source + item1).IndexOf(item1, StringComparison.Ordinal) - (source + item2).IndexOf(item2, StringComparison.Ordinal);
        }

        public static string GetNextToken(this string text, string scope)
        {
            string token;
            var startkrull = "{" + scope;
            var startpowerfx = "<" + scope + "PowerFx|";
            var startexpand = "<" + scope + "expand|";
            var startiif = "<" + scope + "iif|";
            var startsystem = "<" + scope + "system|";
            var startrandom = "<" + scope + "random|";
            if (text.Contains(startkrull) &&
                ComparePositions(text, startkrull, startpowerfx) < 0 &&
                ComparePositions(text, startkrull, startexpand) < 0 &&
                ComparePositions(text, startkrull, startiif) < 0 &&
                ComparePositions(text, startkrull, startsystem) < 0 &&
                ComparePositions(text, startkrull, startrandom) < 0)
            {
                token = GetFirstEnclosedPart(text, "{", "", "}", scope);
            }
            else if (text.Contains(startpowerfx) &&
                ComparePositions(text, startpowerfx, startexpand) < 0 &&
                ComparePositions(text, startpowerfx, startiif) < 0 &&
                ComparePositions(text, startpowerfx, startsystem) < 0 &&
                ComparePositions(text, startpowerfx, startrandom) < 0)
            {
                token = GetFirstEnclosedPart(text, "<", "PowerFx|", ">", scope);
            }
            else if (text.Contains(startexpand) &&
                ComparePositions(text, startexpand, startiif) < 0 &&
                ComparePositions(text, startexpand, startsystem) < 0 &&
                ComparePositions(text, startexpand, startrandom) < 0)
            {
                token = GetFirstEnclosedPart(text, "<", "expand|", ">", scope);
            }
            else if (text.Contains(startiif) &&
                ComparePositions(text, startiif, startsystem) < 0 &&
                ComparePositions(text, startiif, startrandom) < 0)
            {
                token = GetFirstEnclosedPart(text, "<", "iif|", ">", scope);
            }
            else if (text.Contains(startsystem) &&
                ComparePositions(text, startsystem, startrandom) < 0)
            {
                token = GetFirstEnclosedPart(text, "<", "system|", ">", scope);
            }
            else if (text.Contains(startrandom))
            {
                token = GetFirstEnclosedPart(text, "<", "random|", ">", scope);
            }
            else
            {
                token = "";
            }

            return token;
        }

        public static string ExtractExtraFormatTags(this string format, List<string> extraFormats)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return format;
            }
            var originalformat = format;
            var formats = new Dictionary<int, string>();
            while (ContainsAnyTag(format))
            {
                var pos = int.MaxValue;
                var nextFormat = string.Empty;
                foreach (var tag in extraFormatTags)
                {
                    var extraFormat = GetFirstEnclosedPart(format, "<", tag, ">", "");
                    if (!string.IsNullOrEmpty(extraFormat) && format.IndexOf(extraFormat, StringComparison.Ordinal) < pos)
                    {
                        nextFormat = extraFormat;
                        pos = format.IndexOf(extraFormat, StringComparison.Ordinal);
                    }
                }
                if (!string.IsNullOrEmpty(nextFormat))
                {
                    formats.Add(originalformat.IndexOf("<" + nextFormat + ">"), nextFormat);
                    format = format.Replace("<" + nextFormat + ">", "");
                }
            }
            extraFormats.AddRange(formats.OrderBy(t => t.Key).Select(t => t.Value));
            return format;
        }

        public static string FormatByTag(this string text, string formatTag)
        {
            if (formatTag.StartsWith("MaxLen", StringComparison.Ordinal))
            {
                text = FormatLeft(text, formatTag.Replace("MaxLen", "Left").Replace("Left:", "Left|"));   // A few replace for backward compatibility
            }
            else if (formatTag.StartsWith("Left", StringComparison.Ordinal))
            {
                text = FormatLeft(text, formatTag);
            }
            else if (formatTag.StartsWith("Right", StringComparison.Ordinal))
            {
                text = FormatRight(text, formatTag);
            }
            else if (formatTag.StartsWith("Trim", StringComparison.Ordinal))
            {
                text = FormatTrim(text, formatTag);
            }
            else if (formatTag.StartsWith("SubStr", StringComparison.Ordinal))
            {
                text = FormatSubStr(text, formatTag);
            }
            else if (formatTag.StartsWith("Math", StringComparison.Ordinal))
            {
                text = FormatMath(text, formatTag);
            }
            else if (formatTag.StartsWith("Pad", StringComparison.Ordinal))
            {
                text = FormatPad(text, formatTag);
            }
            else if (formatTag.StartsWith("Replace", StringComparison.Ordinal))
            {
                text = FormatReplace(text, formatTag);
            }
            else if (formatTag.StartsWith("Upper", StringComparison.Ordinal))
            {
                text = FormatUpper(text);
            }
            else if (formatTag.StartsWith("Lower", StringComparison.Ordinal))
            {
                text = FormatLower(text);
            }

            return text;
        }

        #endregion Public Static Extensions Methods

        #region Private Static Methods

        private static string GetFirstEnclosedPartStartString(string source, string starttag, string keyword, string endtag, string scope)
        {   // Do this to have or haven't a pipe
            var startidentifier = starttag + scope + keyword;
            if (starttag == "<" && !keyword.EndsWith("|"))
            {
                if (source.Contains(startidentifier + endtag))
                {
                    startidentifier += ">";
                }
                else if (source.Contains(startidentifier + "|"))
                {
                    startidentifier += "|";
                }
            }
            if (source.Contains(startidentifier))
            {
                return source.Substring(source.IndexOf(startidentifier, StringComparison.Ordinal) + 1);
            }
            return null;
        }

        private static bool ContainsAnyTag(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                foreach (var tag in extraFormatTags)
                {
                    if (format.Contains("<" + tag + "|") && format.Contains(">"))
                    {
                        return true;
                    }
                    if (format.Contains("<" + tag + "=") && format.Contains(">"))
                    {
                        return true;
                    }
                    if (format.Contains("<" + tag + ">"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string FormatLeft(string text, string formatTag)
        {
            var lenstr = formatTag.GetSeparatedPart("|", 2);
            int len;
            if (!int.TryParse(lenstr, out len))
            {
                throw new InvalidPluginExecutionException("XRM Tokens left length must be a positive integer (" + lenstr + ")");
            }
            if (text.Length > len)
            {
                text = text.Substring(0, len);
            }
            return text;
        }

        private static string FormatRight(string text, string formatTag)
        {
            var lenstr = formatTag.GetSeparatedPart("|", 2);
            int len;
            if (!int.TryParse(lenstr, out len))
            {
                throw new InvalidPluginExecutionException("XRM Tokens right length must be a positive integer (" + lenstr + ")");
            }
            if (text.Length > len)
            {
                text = text.Substring(text.Length - len);
            }
            return text;
        }

        private static string FormatTrim(string text, string formatTag)
        {
            var trim = formatTag.GetSeparatedPart("|", 1);
            var trimtext = formatTag.GetSeparatedPart("|", 2);
            switch (trim)
            {
                case "Trim":
                    if (string.IsNullOrEmpty(trimtext))
                    {
                        return text.Trim();
                    }
                    else
                    {
                        if (text.StartsWith(trimtext, StringComparison.OrdinalIgnoreCase))
                        {
                            text = text.Substring(trimtext.Length);
                        }
                        if (text.EndsWith(trimtext, StringComparison.OrdinalIgnoreCase))
                        {
                            text = text.Substring(0, text.Length - trimtext.Length);
                        }
                        return text;
                    }
                case "TrimStart":
                    if (string.IsNullOrEmpty(trimtext))
                    {
                        return text.TrimStart();
                    }
                    else
                    {
                        if (text.StartsWith(trimtext, StringComparison.OrdinalIgnoreCase))
                        {
                            return text.Substring(trimtext.Length);
                        }
                        return text;
                    }
                case "TrimEnd":
                    if (string.IsNullOrEmpty(trimtext))
                    {
                        return text.TrimEnd();
                    }
                    else
                    {
                        if (text.EndsWith(trimtext, StringComparison.OrdinalIgnoreCase))
                        {
                            return text.Substring(0, text.Length - trimtext.Length);
                        }
                        return text;
                    }
                default:
                    throw new InvalidPluginExecutionException($"Incorrect Trim: {trim}");
            }
        }

        private static string FormatMath(string text, string formatTag)
        {
            decimal textvalue;
            if (!decimal.TryParse(text.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out textvalue))
            {
                throw new InvalidPluginExecutionException("XRM Tokens math text must be a valid decimal number (" + text + ")");
            }
            var oper = formatTag.GetSeparatedPart("|", 2).ToUpperInvariant();
            decimal value = 0;
            if (oper != "ROUND" && oper != "ABS")
            {
                var valuestr = formatTag.GetSeparatedPart("|", 3);
                if (!decimal.TryParse(valuestr, out value))
                {
                    throw new InvalidPluginExecutionException("XRM Tokens math value must be a valid decimal number (" + valuestr + ")");
                }
            }
            switch (oper)
            {
                case "+":
                    textvalue = textvalue + value;
                    break;

                case "-":
                    textvalue = textvalue - value;
                    break;

                case "*":
                    textvalue = textvalue * value;
                    break;

                case "/":
                    textvalue = textvalue / value;
                    break;

                case "DIV":
                    int rem;
                    textvalue = Math.DivRem((int)textvalue, (int)value, out rem);
                    break;

                case "MOD":
                    int remainder;
                    Math.DivRem((int)textvalue, (int)value, out remainder);
                    textvalue = remainder;
                    break;

                case "ROUND":
                    textvalue = Math.Round(textvalue);
                    break;

                case "ABS":
                    textvalue = Math.Abs(textvalue);
                    break;

                default:
                    throw new InvalidPluginExecutionException("XRM Tokens math operator not valid (" + oper + ")");
            }
            return textvalue.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatPad(string text, string formatTag)
        {
            var dir = formatTag.GetSeparatedPart("|", 2);
            if (dir != "R" && dir != "L")
            {
                throw new InvalidPluginExecutionException("XRM Tokens pad direction must be R or L");
            }
            var lenstr = formatTag.GetSeparatedPart("|", 3);
            var len = 0;
            if (!int.TryParse(lenstr, out len))
            {
                throw new InvalidPluginExecutionException("XRM Tokens pad length must be a positive integer (" + lenstr + ")");
            }
            var pad = formatTag.GetSeparatedPart("|", 4);
            if (string.IsNullOrEmpty(pad))
            {
                pad = " ";
            }
            while (text.Length < len)
            {
                switch (dir)
                {
                    case "R": text = $"{text}{pad}"; break;
                    case "L": text = $"{pad}{text}"; break;
                }
            }

            return text;
        }

        private static string FormatReplace(string text, string formatTag)
        {
            var oldText = formatTag.GetSeparatedPart("|", 2);
            if (string.IsNullOrEmpty(oldText))
            {
                throw new InvalidPluginExecutionException("XRM Tokens replace old must be non-empty");
            }
            var newText = formatTag.GetSeparatedPart("|", 3);
            text = text.Replace(oldText, newText);
            return text;
        }

        private static string FormatSubStr(string text, string formatTag)
        {
            var startstr = formatTag.GetSeparatedPart("|", 2);
            int start;
            if (!int.TryParse(startstr, out start))
            {
                throw new InvalidPluginExecutionException("XRM Tokens substr start must be a positive integer (" + startstr + ")");
            }
            var lenstr = formatTag.GetSeparatedPart("|", 3);
            if (!string.IsNullOrEmpty(lenstr))
            {
                int len;
                if (!int.TryParse(lenstr, out len))
                {
                    throw new InvalidPluginExecutionException("XRM Tokens substr length must be a positive integer (" + lenstr + ")");
                }
                text = text.Substring(start, len);
            }
            else
            {
                text = text.Substring(start);
            }
            return text;
        }

        private static string FormatUpper(string text)
        {
            return text.ToUpper();
        }

        private static string FormatLower(string text)
        {
            return text.ToLower();
        }

        #endregion Private Static Methods
    }
}