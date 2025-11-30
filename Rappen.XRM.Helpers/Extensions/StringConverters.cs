using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Globalization;
using System.Text;

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
    }
}