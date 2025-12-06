using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class StringConverters
    {
        public static object ConvertTo(this string text, AttributeMetadata metadata) => ConvertTo(text, metadata?.AttributeType, metadata);

        public static object ConvertTo(this string text, AttributeTypeCode? attributetypecode, AttributeMetadata metadata = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            var textdecimal = text.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            switch (attributetypecode)
            {
                case AttributeTypeCode.Boolean:
                    if (bool.TryParse(text, out var boolvalue))
                    {
                        return boolvalue;
                    }
                    if (text == "1" || text == "0")
                    {
                        return text == "1";
                    }
                    throw new Exception("Not valid format [True|False] or [1|0]");
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    var entity = string.Empty;
                    var id = Guid.Empty;
                    if (Guid.TryParse(text, out id))
                    {
                        var lookupmeta = metadata as LookupAttributeMetadata;
                        if (lookupmeta?.Targets?.Length == 1)
                        {
                            entity = lookupmeta.Targets[0];
                        }
                    }
                    else if (text.Contains(":"))
                    {
                        if (Guid.TryParse(text.Split(':')[1], out id))
                        {
                            entity = text.Split(':')[0];
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(entity) && !id.Equals(Guid.Empty))
                    {
                        return new EntityReference(entity, id);
                    }
                    throw new Exception("Not valid format [Entity:]Guid");
                case AttributeTypeCode.DateTime:
                    if (DateTime.TryParse(text, out var datetime))
                    {
                        return datetime;
                    }
                    throw new Exception("Not valid format Date[Time]");
                case AttributeTypeCode.Decimal:
                    if (decimal.TryParse(textdecimal, out var decimalvalue))
                    {
                        return decimalvalue;
                    }
                    break;

                case AttributeTypeCode.Double:
                    if (double.TryParse(textdecimal, out var doublevalue))
                    {
                        return doublevalue;
                    }
                    break;

                case AttributeTypeCode.Integer:
                case AttributeTypeCode.BigInt:
                    if (int.TryParse(text, out var intvalue))
                    {
                        return intvalue;
                    }
                    break;

                case AttributeTypeCode.Money:
                    if (decimal.TryParse(textdecimal, out var moneyvalue))
                    {
                        return new Money(moneyvalue);
                    }
                    break;

                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    if (int.TryParse(text, out var pickvalue))
                    {
                        return new OptionSetValue(pickvalue);
                    }
                    break;

                case AttributeTypeCode.Virtual:
                    if (metadata is MultiSelectPicklistAttributeMetadata multi)
                    {
                        var result = new OptionSetValueCollection();
                        foreach (var optionvalue in text.Split(';'))
                        {
                            if (int.TryParse(optionvalue, out var value))
                            {
                                result.Add(new OptionSetValue(value));
                            }
                        }
                        return result;
                    }
                    else
                    {
                        throw new Exception($"Not supporting {metadata?.ToString().Replace("Type", "")}");
                    }

                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return text;

                case AttributeTypeCode.Uniqueidentifier:
                    if (Guid.TryParse(text, out var guidvalue))
                    {
                        return guidvalue;
                    }
                    break;

                case AttributeTypeCode.PartyList:
                case AttributeTypeCode.CalendarRules:
                case AttributeTypeCode.EntityName:
                case AttributeTypeCode.ManagedProperty:
                    throw new Exception($"Not supporting {attributetypecode}");
            }
            throw new Exception($"Not valid {attributetypecode}");
        }

        /// <summary>
        /// Converts the input string to camelCase.
        /// Non-alphanumeric characters are treated as separators; the next letter is capitalized.
        /// Digits are preserved and consecutive separators are collapsed.
        /// Returns the original value for null or empty input.
        /// </summary>
        /// <param name="text">The input text to convert.</param>
        /// <returns>The camelCase representation of the input.</returns>
        public static string ToCamelCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var sb = new StringBuilder(text.Length);
            var makeUpper = false;
            var hasWrittenAny = false;

            foreach (var ch in text)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (!hasWrittenAny)
                    {
                        sb.Append(char.ToLowerInvariant(ch));
                        hasWrittenAny = true;
                        makeUpper = false;
                    }
                    else
                    {
                        sb.Append(makeUpper ? char.ToUpperInvariant(ch) : ch);
                        makeUpper = false;
                    }
                }
                else
                {
                    makeUpper = hasWrittenAny;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the input string to PascalCase (first letter uppercase).
        /// Non-alphanumeric characters are treated as separators; the next letter is capitalized.
        /// Digits are preserved and consecutive separators are collapsed.
        /// Returns the original value for null or empty input.
        /// </summary>
        /// <param name="text">The input text to convert.</param>
        /// <returns>The PascalCase representation of the input.</returns>
        public static string ToPascalCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var sb = new StringBuilder(text.Length);
            var makeUpper = true; // first letter should be upper
            foreach (var ch in text)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(makeUpper ? char.ToUpperInvariant(ch) : ch);
                    makeUpper = false;
                }
                else
                {
                    makeUpper = true;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the input string to Title Case (aka Proper Case),
        /// capitalizing the first letter of each word and lowercasing the rest, except words already ALL UPPERCASE (acronyms), which are preserved.
        /// Treats sequences of letters/digits and apostrophes as words; preserves punctuation and contractions.
        /// Returns the original value for null or empty input.
        /// </summary>
        /// <param name="text">The input text to convert.</param>
        /// <returns>The Title Case representation of the input.</returns>
        public static string ToTitleCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var source = text.ToCharArray();
            var result = new char[source.Length];

            var newWord = true;
            var preserveUpper = false;

            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];

                if (char.IsLetterOrDigit(c) || c == '\'')
                {
                    if (newWord)
                    {
                        // Determine the word span [i, end)
                        var start = i;
                        var end = i;
                        var hasLetter = false;
                        var allUpper = true;

                        while (end < source.Length)
                        {
                            var wc = source[end];
                            if (char.IsLetterOrDigit(wc) || wc == '\'')
                            {
                                if (char.IsLetter(wc))
                                {
                                    hasLetter = true;
                                    if (!char.IsUpper(wc))
                                    {
                                        allUpper = false;
                                    }
                                }
                                end++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        preserveUpper = hasLetter && allUpper;

                        // First character of the word
                        if (char.IsLetter(c))
                        {
                            result[i] = char.ToUpperInvariant(c);
                        }
                        else
                        {
                            result[i] = c;
                        }

                        newWord = false;
                    }
                    else
                    {
                        if (char.IsLetter(c))
                        {
                            result[i] = preserveUpper ? c : char.ToLowerInvariant(c);
                        }
                        else
                        {
                            result[i] = c;
                        }
                    }
                }
                else
                {
                    result[i] = c;
                    newWord = true;
                    preserveUpper = false;
                }
            }

            return new string(result);
        }

        public static string RemoveTrailingDigits(this string input) =>
            string.IsNullOrEmpty(input) ? string.Empty :
            new string(input.Reverse()
                .SkipWhile(char.IsDigit)
                .Reverse()
                .ToArray());

        public static string KeepLettersAndDigits(this string? input) =>
            string.IsNullOrEmpty(input) ? string.Empty :
            new string(input.Where(char.IsLetterOrDigit).ToArray());

        /// <summary>
        /// Acronym / short-name generator.
        /// Designed for FetchXML aliases and general short identifiers.
        ///
        /// Behavior:
        /// - Y is a vowel.
        /// - Diacritics & ligatures removed via NFKD (ÅÄÖ→AAO, Æ→AE, Ø→O, Œ→OE, ü→u; plus ß→ss).
        /// - Word splitting: non-alnum boundaries + Camel/PascalCase.
        /// - If length == 0: return acronym (initials), uppercased unless preserveCasing=true.
        /// - Else:
        ///   Prefix = initials of all words except LAST (single-word: first letter).
        ///   Fill Phase 1: consonants (non-AEIOUY) from words LAST→FIRST; per word left→right (skip first char).
        ///   Fill Phase 2: vowels from end across words LAST→FIRST; per word right→left (skip first char).
        ///   Emit in natural left→right order.
        ///   Uppercase unless preserveCasing=true.
        /// - If returnOriginalIfLonger && length > input.Length:
        ///   returns original unchanged (or sanitized if identifierSafe = true).
        /// - If identifierSafe = true: ASCII-only [A-Za-z0-9_], and never starts with a digit (prefix '_' if needed).
        /// </summary>
        /// <param name="input">Source text (e.g., entity/table display or logical name).</param>
        /// <param name="length">Target length. 0 returns acronym.</param>
        /// <param name="identifierSafe">Force C#-identifier-safe output (use for FetchXML alias).</param>
        /// <param name="preserveCasing">Keep original casing; otherwise uppercase shortened outputs.</param>
        /// <param name="returnOriginalIfLonger">If true and length > input.Length, return original (or sanitized if identifierSafe).</param>
        /// <param name="includeAllWordInitials">If true, include initials from all words as mandatory prefix.</param>
        public static string ToAcronym(this string input, int length = 0,
                                          bool identifierSafe = false,
                                          bool preserveCasing = false,
                                          bool returnOriginalIfLonger = true,
                                          bool includeAllWordInitials = false)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return identifierSafe ? "_" : string.Empty;
            }

            // If asked longer than original, keep original (sanitize iff identifierSafe)
            if (returnOriginalIfLonger && length > input.Length)
            {
                return identifierSafe ? MakeIdentifierSafe(input) : input;
            }

            var normalized = RemoveDiacritics(input);

            // Split words: non-alnum + CamelCase
            var words = Regex.Split(normalized, @"[^A-Za-z0-9]+")
                             .SelectMany(w => Regex.Split(w, @"(?<!^)(?=[A-Z])"))
                             .Where(w => !string.IsNullOrEmpty(w))
                             .ToList();
            if (words.Count == 0)
            {
                return identifierSafe ? "_" : string.Empty;
            }

            // Acronym (initials)
            var acronym = string.Concat(words.Select(w => w[0]));

            if (length == 0)
            {
                var res0 = preserveCasing ? acronym : acronym.ToUpperInvariant();
                return identifierSafe ? MakeIdentifierSafe(res0) : res0;
            }

            // Prefix selection
            string prefix;
            if (includeAllWordInitials)
            {
                // Use initials from all words (e.g., Jonas and Linda -> JAL)
                prefix = acronym;
            }
            else
            {
                // Prefix = initials of all words except LAST (single-word: first letter)
                prefix = (words.Count == 1)
                    ? words[0][0].ToString()
                    : string.Concat(words.Take(words.Count - 1).Select(w => w[0]));
            }

            var needed = Math.Max(0, length - prefix.Length);
            if (needed == 0)
            {
                var baseRes = preserveCasing ? prefix : prefix.ToUpperInvariant();
                return identifierSafe ? MakeIdentifierSafe(baseRes) : baseRes;
            }

            // Fill from LAST→FIRST: include first char of LAST word (to match acronym for minimal lengths),
            // then consonants L→R, then vowels R→L (Y is vowel)
            var fill = new StringBuilder();
            const string vowels = "AEIOUYaeiouy";

            var reversed = words.AsEnumerable().Reverse().ToList();
            for (int widx = 0; widx < reversed.Count && fill.Length < needed; widx++)
            {
                var w = reversed[widx];

                // Special-case: for the very last word in natural order (first in reversed),
                // include its first character before other fill rules. This ensures length == number of words
                // yields the expected acronym.
                if (!includeAllWordInitials && widx == 0 && w.Length > 0 && fill.Length < needed)
                {
                    fill.Append(w[0]);
                }

                if (fill.Length >= needed)
                {
                    break;
                }

                var chars = w.Skip(1).ToList(); // skip first char for subsequent rules

                // Consonants L->R
                foreach (var c in chars)
                {
                    if (fill.Length >= needed)
                    {
                        break;
                    }

                    if (!vowels.Contains(c))
                    {
                        fill.Append(c);
                    }
                }
                if (fill.Length >= needed)
                {
                    break;
                }

                // Vowels from end R->L
                for (var i = chars.Count - 1; i >= 0 && fill.Length < needed; i--)
                {
                    var c = chars[i];
                    if (vowels.Contains(c))
                    {
                        fill.Append(c);
                    }
                }
            }

            var result = (prefix + fill).Substring(0, Math.Min(length, prefix.Length + fill.Length));
            if (!preserveCasing)
            {
                result = result.ToUpperInvariant();
            }

            return identifierSafe ? MakeIdentifierSafe(result) : result;
        }

        // --- helpers ---

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormKD); // NFKD
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark &&
                    uc != UnicodeCategory.SpacingCombiningMark &&
                    uc != UnicodeCategory.EnclosingMark)
                {
                    sb.Append(ch);
                }
            }
            // Common case-fold not covered by NFKD
            return sb.ToString().Replace("ß", "ss").Normalize(NormalizationForm.FormC);
        }

        private static string MakeIdentifierSafe(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "_";
            }

            s = Regex.Replace(s, @"[^A-Za-z0-9_]", "_");
            if (char.IsDigit(s[0]))
            {
                s = "_" + s;
            }

            return s.Length == 0 ? "_" : s;
        }
    }
}