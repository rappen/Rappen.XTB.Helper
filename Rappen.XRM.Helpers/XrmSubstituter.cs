using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace Rappen.XRM.Helpers
{
    public static class XrmSubstituter
    {
        #region Public extensions methods

        public static string Substitute(this Entity entity, IOrganizationService service, string text) => Substitute(entity, new GenericBag(service), text);

        public static string Substitute(this Entity entity, IBag bag, string text) => Substitute(entity, bag, text, 0, string.Empty);

        public static string Substitute(this Entity entity, IBag bag, string text, int sequence) => Substitute(entity, bag, text, sequence, string.Empty);

        public static string Substitute(this Entity entity, IBag bag, string text, int sequence, string scope) => Substitute(entity, bag, text, sequence, scope, false);

        public static string Substitute(this Entity entity, IBag bag, string text, int sequence, string scope, bool supressinvalidattributepaths) => Substitute(entity, bag, text, sequence, scope, null, supressinvalidattributepaths);

        #endregion Public extensions methods

        #region Private extensions methods

        private static string Substitute(this Entity entity, IBag bag, string text, int sequence, string scope, Dictionary<string, string> replacepatterns, bool supressinvalidattributepaths)
        {
            bag.Logger.StartSection("Substitute " + scope);
            if (text == null)
            {
                text = string.Empty;
            }
            // Halvdan lösning för att hantera taggar som enkodats...
            if (text.Contains("&lt;expand|") || text.Contains("&lt;iif|") || text.Contains("&lt;system|"))
            {
                text = text.Replace("&lt;", "<").Replace("&gt;", ">");
            }

            // Looking for "all" or tags with a specific entity scope?
            var starttag = string.IsNullOrEmpty(scope) ? "" : "" + scope + ":";

            var token = GetNextToken(text, starttag);
            while (!string.IsNullOrWhiteSpace(token))
            {
                bag.Logger.Log($"Found token: {token}");
                if (token.StartsWith(starttag + "expand|", StringComparison.Ordinal))
                {
                    text = entity.Expand(bag, text, replacepatterns, token);
                }
                else if (token.StartsWith(starttag + "iif|", StringComparison.Ordinal))
                {
                    text = entity.EvaluateIif(bag, text, scope, replacepatterns, token);
                }
                else if (token.ToLowerInvariant().StartsWith(starttag + "system|", StringComparison.Ordinal))
                {
                    text = ReplaceSystem(bag, text, token);
                }
                else if (token.ToLowerInvariant().StartsWith(starttag + "random|", StringComparison.Ordinal))
                {
                    text = ReplaceRandom(bag, text, token);
                }
                else
                {
                    text = entity.Replace(bag, text, scope, replacepatterns, supressinvalidattributepaths, token);
                }

                token = GetNextToken(text, starttag);
            }

            if (text.Contains("%STARTKRULL_%") && text.Contains("%SLUTKRULL_%"))
            {
                text = text.Replace("%STARTKRULL_%", "{").Replace("%SLUTKRULL_%", "}");
            }

            if (sequence > 0)
            {
                text = ReplaceSequence(text, sequence);
            }

            bag.Logger.EndSection();
            return text;
        }

        #endregion Private extensions methods

        #region Private static properties

        private static List<string> extraFormatTags = new List<string>() { "MaxLen", "Pad", "Left", "Right", "Trim", "TrimStart", "TrimEnd", "SubStr", "Replace", "Math" };
        private static Dictionary<string, string> xmlReplacePatterns = new Dictionary<string, string>() { { "&", "&amp;" }, { "<", "&lt;" }, { ">", "&gt;" }, { "\"", "&quot;" }, { "'", "&apos;" } };
        private static Random random;

        #endregion Private static properties

        #region Private static methods

        private static int ComparePositions(string source, string item1, string item2)
        {
            return (source + item1).IndexOf(item1, StringComparison.Ordinal) - (source + item2).IndexOf(item2, StringComparison.Ordinal);
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
            var lenstr = GetSeparatedPart(formatTag, "|", 2);
            int len;
            if (!int.TryParse(lenstr, out len))
            {
                throw new InvalidPluginExecutionException("Substitute left length must be a positive integer (" + lenstr + ")");
            }
            if (text.Length > len)
            {
                text = text.Substring(0, len);
            }
            return text;
        }

        private static string FormatRight(string text, string formatTag)
        {
            var lenstr = GetSeparatedPart(formatTag, "|", 2);
            int len;
            if (!int.TryParse(lenstr, out len))
            {
                throw new InvalidPluginExecutionException("Substitute right length must be a positive integer (" + lenstr + ")");
            }
            if (text.Length > len)
            {
                text = text.Substring(text.Length - len);
            }
            return text;
        }

        private static string FormatTrim(string text, string formatTag)
        {
            var trim = GetSeparatedPart(formatTag, "|", 1);
            var trimtext = GetSeparatedPart(formatTag, "|", 2);
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
                throw new InvalidPluginExecutionException("Substitute math text must be a valid decimal number (" + text + ")");
            }
            var oper = GetSeparatedPart(formatTag, "|", 2).ToUpperInvariant();
            decimal value = 0;
            if (oper != "round" && oper != "abs")
            {
                var valuestr = GetSeparatedPart(formatTag, "|", 3);
                if (!decimal.TryParse(valuestr, out value))
                {
                    throw new InvalidPluginExecutionException("Substitute math value must be a valid decimal number (" + valuestr + ")");
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
                    throw new InvalidPluginExecutionException("Substitute math operator not valid (" + oper + ")");
            }
            return textvalue.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatPad(string text, string formatTag)
        {
            var dir = GetSeparatedPart(formatTag, "|", 2);
            if (dir != "R" && dir != "L")
            {
                throw new InvalidPluginExecutionException("Substitute pad direction must be R or L");
            }
            var lenstr = GetSeparatedPart(formatTag, "|", 3);
            var len = 0;
            if (!int.TryParse(lenstr, out len))
            {
                throw new InvalidPluginExecutionException("Substitute pad length must be a positive integer (" + lenstr + ")");
            }
            var pad = GetSeparatedPart(formatTag, "|", 4);
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
            var oldText = GetSeparatedPart(formatTag, "|", 2);
            if (string.IsNullOrEmpty(oldText))
            {
                throw new InvalidPluginExecutionException("Substitute replace old must be non-empty");
            }
            var newText = GetSeparatedPart(formatTag, "|", 3);
            text = text.Replace(oldText, newText);
            return text;
        }

        private static string FormatSubStr(string text, string formatTag)
        {
            var startstr = GetSeparatedPart(formatTag, "|", 2);
            int start;
            if (!int.TryParse(startstr, out start))
            {
                throw new InvalidPluginExecutionException("Substitute substr start must be a positive integer (" + startstr + ")");
            }
            var lenstr = GetSeparatedPart(formatTag, "|", 3);
            if (!string.IsNullOrEmpty(lenstr))
            {
                int len;
                if (!int.TryParse(lenstr, out len))
                {
                    throw new InvalidPluginExecutionException("Substitute substr length must be a positive integer (" + lenstr + ")");
                }
                text = text.Substring(start, len);
            }
            else
            {
                text = text.Substring(start);
            }
            return text;
        }

        private static string GetFirstEnclosedPart(string source, string starttag, string keyword, string endtag, string scope)
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

        private static string GetNextToken(string text, string scope)
        {
            string token;
            var startkrull = "{" + scope;
            var startexpand = "<" + scope + "expand|";
            var startiif = "<" + scope + "iif|";
            var startsystem = "<" + scope + "system|";
            var startrandom = "<" + scope + "random|";
            if (text.Contains(startkrull) &&
                ComparePositions(text, startkrull, startexpand) < 0 &&
                ComparePositions(text, startkrull, startiif) < 0 &&
                ComparePositions(text, startkrull, startsystem) < 0 &&
                ComparePositions(text, startkrull, startrandom) < 0)
            {
                token = GetFirstEnclosedPart(text, "{", "", "}", scope);
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

        private static string GetSeparatedPart(string source, string separator, int partno)
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

        private static Guid getUserId(IBag bag)
        {
            Guid userid = Guid.Empty;
            try
            {
                if (bag.Service is OrganizationServiceProxy svc)
                {
                    userid = svc.CallerId;
                }
            }
            catch (InvalidPluginExecutionException)
            {
                // No idea really why this could fail...
            }
            if (userid.Equals(Guid.Empty))
            {
                userid = ((WhoAmIResponse)bag.Service.Execute(new WhoAmIRequest())).UserId;
            }
            return userid;
        }

        private static string MultiReplace(string value, Dictionary<string, string> replacepatterns)
        {
            if (replacepatterns.Count == 1 && replacepatterns["XmlAndSpecialCharsReplacePatterns"] == "1")
            {
                value = MultiReplace(value, xmlReplacePatterns);
                var result = new StringBuilder();
                foreach (char c in value)
                {
                    result.Append((int)c >= 0x80 ? String.Format("&#{0};", (int)c) : c.ToString());
                }
                value = result.ToString();
            }
            else
            {
                foreach (string oldpattern in replacepatterns.Keys)
                {
                    if (value.Contains(oldpattern))
                    {
                        value = value.Replace(oldpattern, replacepatterns[oldpattern]);
                    }
                }
            }
            return value;
        }

        private static string Expand(this Entity entity, IBag bag, string text, Dictionary<string, string> replacepatterns, string token)
        {
            bag.Logger.StartSection("Expand " + token);
            // Check token <expand|child-entity|child-relation-attribute|format-string|order|separator|distinct>
            // Example for an account in CEM, lists courses booked by contacts associated with the account:
            // {name} Owners:\n<expand|contact|parentcustomerid|<expand|incident|customerid|{ownerid.fullname}||\n|true>||\n|true>

            // Extract current token
            var child = GetSeparatedPart(token, "|", 2);
            var relation = GetSeparatedPart(token, "|", 3);
            var format = GetSeparatedPart(token, "|", 4);
            var order = GetSeparatedPart(token, "|", 5);
            var separator = GetSeparatedPart(token, "|", 6).Replace("\\n", "\n").Replace("\\r", "\r");
            var distinct = GetSeparatedPart(token, "|", 7).ToLowerInvariant();
            var activeonly = GetSeparatedPart(token, "|", 8).ToLowerInvariant();
            var strMaxNumber = GetSeparatedPart(token, "|", 9);
            var nMaxNumber = int.MaxValue;
            if (!string.IsNullOrWhiteSpace(strMaxNumber))
            {
                if (!int.TryParse(strMaxNumber, out nMaxNumber))
                {
                    nMaxNumber = int.MaxValue;
                }
                else
                {
                    bag.Logger.Log($"Found max number in Expand: {nMaxNumber}");
                }
            }

            var orders = new List<OrderExpression>();
            if (!string.IsNullOrWhiteSpace(order))
            {
                foreach (string orderattr in order.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(orderattr))
                    {
                        var orderattribute = orderattr.Trim();
                        var ordertype = OrderType.Ascending;
                        if (orderattribute.Contains("/"))
                        {
                            var ordertypestr = orderattribute.Split('/')[1];
                            switch (ordertypestr.ToUpperInvariant())
                            {
                                case "ASC":
                                    break;

                                case "DESC":
                                    ordertype = OrderType.Descending;
                                    break;

                                default:
                                    throw new InvalidPluginExecutionException("Invalid order by directive: " + orderattr);
                            }
                            orderattribute = orderattribute.Split('/')[0];
                        }
                        orders.Add(new OrderExpression(orderattribute.Trim(), ordertype));
                    }
                }
            }
            var cExpanded = entity.GetRelating(bag, child, relation, !activeonly.Equals("false", StringComparison.OrdinalIgnoreCase), null, orders.ToArray(), new ColumnSet(true), true);

            var subValues = new List<string>();
            var nIndex = 1;
            foreach (var expanded in cExpanded.Entities)
            {
                var subvalue = expanded.Substitute(bag, format, nIndex, string.Empty, replacepatterns, false);
                if (!string.IsNullOrWhiteSpace(subvalue) && (!distinct.Equals("true", StringComparison.OrdinalIgnoreCase) || !subValues.Contains(subvalue)))
                {
                    subValues.Add(subvalue.Replace("##", nIndex.ToString()));
                }
                if (++nIndex > nMaxNumber)
                {
                    break;
                }
            }
            if (orders.Count == 0)
            {
                subValues.Sort();
            }
            var value = string.Join(separator, subValues);

            bag.Logger.Log($"Replacing <{token}> with {value}");
            bag.Logger.EndSection();
            return text.Replace("<" + token + ">", value);
        }

        private static string SystemNow(string format)
        {
            string value;
            if (string.IsNullOrWhiteSpace(format))
            {
                value = DateTime.Now.ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                value = DateTime.Now.ToString(format);
            }
            return value;
        }

        private static string SystemToday(string format)
        {
            string value;
            if (string.IsNullOrWhiteSpace(format))
            {
                value = DateTime.Today.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                value = DateTime.Today.ToString(format);
            }
            return value;
        }

        private static string SystemUser(this IBag bag, string token)
        {
            string value = "";
            var userid = getUserId(bag);
            if (!userid.Equals(Guid.Empty))
            {
                var user = bag.Service.Retrieve("systemuser", userid, new ColumnSet(true));
                bag.Logger.Log($"Retrieved user: {user.ToStringExt(bag.Service)}");
                value = user.Substitute(bag, token);
            }
            return value;
        }

        private static string SystemChars(string format)
        {
            return format
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        private static string EvaluateIif(this Entity entity, IBag bag, string text, string scope, Dictionary<string, string> replacepatterns, string token)
        {
            bag.Logger.StartSection("EvaluateIif " + token);
            // <iif|value1|operator|value2|trueresult|falseresult>
            // value1, value2, trueresult and falseresult may be krulled
            // operator must be either of: eq neq lt gt le ge
            // Example: <iif|{firstname}|gt|Kalle|{firstname} är sent i alfabetet|{firstname} kommer tidigt>

            string value1 = GetSeparatedPart(token, "|", 2);
            string oper = GetSeparatedPart(token, "|", 3);
            string value2 = GetSeparatedPart(token, "|", 4);
            string trueresult = GetSeparatedPart(token, "|", 5);
            string falseresult = GetSeparatedPart(token, "|", 6);
            value1 = entity.Substitute(bag, value1, 0, scope);
            value2 = entity.Substitute(bag, value2, 0, scope);
            bool numeric = false;
            decimal decValue1 = 0;
            decimal decValue2 = 0;

            // Sometimes empty string is passed as a value, in this case it should be interpreted as 0
            if (!string.IsNullOrEmpty(value1))
            {
                numeric = decimal.TryParse(value1, out decValue1);
            }
            else
            {
                decValue1 = 0;
                numeric = true;
            }

            // Sometimes empty string is passed as a value, in this case it should be interpreted as 0
            if (!string.IsNullOrEmpty(value2))
            {
                numeric &= decimal.TryParse(value2, out decValue2);
            }
            else
            {
                decValue2 = 0;
                numeric &= true;
            }

            bool evaluation = false;
            switch (oper)
            {
                case "eq":
                    evaluation = numeric ? decValue1 == decValue2 : value1 == value2;
                    break;

                case "neq":
                    evaluation = numeric ? decValue1 != decValue2 : value1 != value2;
                    break;

                case "lt":
                    evaluation = numeric ? decValue1 < decValue2 : string.Compare(value1, value2, StringComparison.InvariantCulture) < 0;
                    break;

                case "gt":
                    evaluation = numeric ? decValue1 > decValue2 : string.Compare(value1, value2, StringComparison.InvariantCulture) > 0;
                    break;

                case "le":
                    evaluation = numeric ? decValue1 <= decValue2 : string.Compare(value1, value2, StringComparison.InvariantCulture) <= 0;
                    break;

                case "ge":
                    evaluation = numeric ? decValue1 >= decValue2 : string.Compare(value1, value2, StringComparison.InvariantCulture) >= 0;
                    break;

                default:
                    throw new InvalidPluginExecutionException("Invalid operator \"" + oper + "\"");
            }
            string result = entity.Substitute(bag, evaluation ? trueresult : falseresult, 0, scope, replacepatterns, false);
            bag.Logger.EndSection();
            return text.Replace("<" + token + ">", result);
        }

        private static string Replace(this Entity entity, IBag bag, string text, string scope, Dictionary<string, string> replacepatterns, bool supressinvalidattributepaths, string token)
        {
            bag.Logger.StartSection("Replace " + token);

            var attributepath = token;
            if (ComparePositions(token, ":", "<") < 0 &&    // Det finns kolon och det är inte en del av iif, expand etc (som börjar med < )
                ComparePositions(token, ":", "|") < 0)      // Det finns kolon och det är inte en del av formatsträng (som börjar med | )
            {
                // Separate namespace and attribute-path from token name
                attributepath = token.Substring(token.IndexOf(':') + 1);             // "businessunitid.createdon|yyyy-MM-dd"
            }
            // Extract format string
            string format = null;
            if (!attributepath.Contains("<expand|") && attributepath.Contains("|"))
            {
                format = attributepath.Substring(attributepath.IndexOf('|') + 1);                   // "yyyy-MM-dd"
                attributepath = attributepath.Substring(0, attributepath.IndexOf('|'));             // "businessunitid.createdon"
            }

            // Extract "next" attribute in the path
            var attribute = entity.Contains(attributepath) ? attributepath : attributepath.Split('.')[0];

            var value = string.Empty;
            if (entity.Contains(attribute)) // Attribute exists
            {
                if ((entity.Attributes[attribute] is EntityReference ||                       // Attribute is a reference
                     entity.Attributes[attribute] is Guid) && attributepath.Contains("."))   // References from intersect tables are only guids
                {
                    // Traverse down through the reference list
                    var attributepos = 2;
                    var finalattribute = attributepath;
                    var tmp = GetSeparatedPart(attributepath, ".", attributepos);
                    while (!string.IsNullOrWhiteSpace(tmp))
                    {
                        finalattribute = tmp; // "createdon"
                        attributepos++;
                        tmp = GetSeparatedPart(attributepath, ".", attributepos);
                    }
                    Entity deRef = null;
                    try
                    {
                        if (format == "<value>" && attributepath.IndexOf('.') < 0)
                        {
                            deRef = entity;
                            bag.Logger.Log($"Get attribute off current entity (token = {token})");
                        }
                        else
                        {
                            var cols = new ColumnSet();
                            if (!finalattribute.StartsWith("<") && !finalattribute.StartsWith("&lt;"))
                            {
                                cols.AddColumn(finalattribute);
                            }

                            var strAttributeRelated = attributepath.Replace("." + finalattribute, "");
                            deRef = entity.GetRelated(bag, strAttributeRelated, cols);
                        }
                    }
                    catch (FaultException<OrganizationServiceFault> ex)
                    {
                        if (!supressinvalidattributepaths)
                        {
                            throw;
                        }
                        bag.Logger.Log($"Invalid path '{token}' is supressed: {ex.Message}");
                    }
                    if (deRef != null)
                    {
                        // Reference found, get the requested attribute from it
                        bag.Logger.Log($"Retrieved related {deRef.LogicalName}");
                        if (format == "<value>")
                        {
                            var basevalue = deRef.PropertyAsBaseType(finalattribute, "", true);
                            if (basevalue is IEnumerable<int> manyint)
                            {
                                value = string.Join(";", manyint.Select(n => n.ToString()));
                            }
                            else
                            {
                                value = basevalue.ToString();// "2010-05-14T..."
                            }
                        }
                        else if (finalattribute.StartsWith("<expand|"))
                        {
                            value = deRef.Substitute(bag, finalattribute, 0, scope);
                        }
                        else
                        {
                            value = deRef.PropertyAsString(bag, finalattribute, "", true, format);     // "2010-05-14"
                        }
                    }
                }
                else
                {
                    // Get attribute text
                    value = entity.PropertyAsString(bag, attribute, "", true, format);
                }
            }
            if (replacepatterns != null && replacepatterns.Count > 0)
            {
                value = MultiReplace(value, replacepatterns);
            }

            if (!string.IsNullOrEmpty(value))
            {
                if (value.Contains(string.Concat("{", token, "}")))
                {   // Prevents recursion within this token, if its value contains the token
                    value = value.Replace(
                        string.Concat("{", token, "}"),
                        string.Concat("%STARTKRULL_%", token, "%SLUTKRULL_%"));
                }
            }

            // Only replace first (current) occurrence of ph, that is why we don't use string.Replace.
            token = string.Concat("{", token, "}");
            var phstart = text.IndexOf(token, StringComparison.InvariantCulture);
            var phlength = token.Length;
            bag.Logger.Log($"Replacing {token} with {value}");
            text = text.Substring(0, phstart) + value + text.Substring(phstart + phlength);
            bag.Logger.Log($"Result: {text}");
            bag.Logger.EndSection();

            return text;
        }

        private static string ReplaceSystem(IBag bag, string text, string token)
        {
            bag.Logger.StartSection("ReplaceSystem " + token);
            string systemtoken = GetSeparatedPart(token, "|", 2).ToLowerInvariant();
            string format = GetSeparatedPart(token, "|", 3);
            bag.Logger.Log($"SystemToken: {systemtoken} Format: {format}");

            string value = "";
            switch (systemtoken.ToUpperInvariant())
            {
                case "NOW":
                    value = SystemNow(format);
                    break;

                case "TODAY":
                    value = SystemToday(format);
                    break;

                case "USER":
                    value = bag.SystemUser(format);
                    break;

                case "CHAR":
                    value = SystemChars(format);
                    break;

                default:
                    throw new InvalidPluginExecutionException("Unknown system token: " + systemtoken);
            }

            bag.Logger.Log($"Replacing <{token}> with {value}");
            bag.Logger.EndSection();
            return text.ReplaceFirstOnly("<" + token + ">", value);
        }

        private static string ReplaceRandom(IBag bag, string text, string token)
        {
            bag.Logger.StartSection("ReplaceRandom " + token);
            string type = GetSeparatedPart(token, "|", 2);
            string param1 = GetSeparatedPart(token, "|", 3);
            string param2 = GetSeparatedPart(token, "|", 4);
            bag.Logger.Log($"Random: {type} Params: {param1}, {param2}");

            var value = string.Empty;
            if (random == null)
            {
                random = new Random();
            }
            switch (type.ToUpperInvariant())
            {
                case "TEXT":
                    value = RandomText(param1, param2);
                    break;

                case "NUMBER":
                    value = RandomNumber(param1, param2);
                    break;

                case "DATE":
                    value = RandomDate(param1, param2);
                    break;

                case "GUID":
                    value = Guid.NewGuid().ToString();
                    break;

                default:
                    throw new InvalidPluginExecutionException($"Invalid random type ({type})");
            }
            bag.Logger.Log($"Replacing <{token}> with {value}");
            bag.Logger.EndSection();
            return text.ReplaceFirstOnly("<" + token + ">", value);
        }

        private static string RandomText(string lengthstr, string characters)
        {
            int.TryParse(lengthstr, out var length);
            if (length <= 0)
            {
                throw new InvalidPluginExecutionException("Random length must be a positive integer (" + lengthstr + ")");
            }
            if (string.IsNullOrWhiteSpace(characters))
            {
                characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            }
            var charlength = characters.Length;
            var result = string.Empty;
            for (int i = 0; i < length; i++)
            {
                var randval = random.Next(charlength);
                result += characters[randval];
            }
            return result;
        }

        private static string RandomNumber(string fromstr, string tostr)
        {
            if (!int.TryParse(fromstr, out int from))
            {
                from = int.MaxValue - 1;
            }
            if (!int.TryParse(tostr, out int to))
            {
                to = from;
                from = 0;
            }
            if (from > to)
            {
                throw new InvalidPluginExecutionException($"Number area is incorrect ({fromstr} - {tostr})");
            }
            return random.Next(from, to + 1).ToString();
        }

        private static string RandomDate(string fromstr, string tostr)
        {
            if (!DateTime.TryParse(fromstr, out DateTime from))
            {
                from = DateTime.MinValue;
            }
            if (!DateTime.TryParse(tostr, out DateTime to))
            {
                to = DateTime.MaxValue;
            }
            if (from > to)
            {
                throw new InvalidPluginExecutionException($"Date area is incorrect ({from} - {to})");
            }
            var datearea = (to - from).TotalDays;
            var days = random.Next((int)datearea);
            var randomday = from.AddDays(days);
            return randomday.ToString("d");
        }

        private static string ReplaceFirstOnly(this string theString, string oldValue, string newValue)
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

        private static string ReplaceSequence(string text, int sequence)
        {
            var num = GetFirstEnclosedPart(text, "[", "#", "]", string.Empty);
            while (!string.IsNullOrWhiteSpace(num))
            {
                var startnostr = "0" + GetSeparatedPart(num, "|", 2);
                if (!int.TryParse(startnostr, out int startno))
                {
                    throw new InvalidPluginExecutionException($"Sequence start value invalid: {startnostr}");
                }
                if (startno > 0)
                {
                    startno--;
                }
                var format = GetSeparatedPart(num, "|", 3);
                var currentvalue = startno + sequence;
                text = text.Replace("[" + num + "]", currentvalue.ToString(format));
                num = GetFirstEnclosedPart(text, "[", "#", "]", string.Empty);
            }

            return text;
        }

        #endregion Private static methods

        #region Internal static methods

        internal static string ExtractExtraFormatTags(string format, List<string> extraFormats)
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

        internal static string FormatByTag(string text, string formatTag)
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

            return text;
        }

        #endregion Internal static methods
    }
}