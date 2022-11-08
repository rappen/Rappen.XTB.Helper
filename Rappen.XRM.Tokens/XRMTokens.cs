using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

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
        private static Dictionary<string, string> xmlReplacePatterns = new Dictionary<string, string>() { { "&", "&amp;" }, { "<", "&lt;" }, { ">", "&gt;" }, { "\"", "&quot;" }, { "'", "&apos;" } };

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
                    var subsubstitute = Tokens(entity, bag, token.Substring(8), sequence, scope, replacepatterns, supressinvalidattributepaths);
                    var pfxvalue = Power.Fx.PowerFxHelpers.Eval(subsubstitute);
                    text = text.ReplaceFirstOnly("<" + token + ">", pfxvalue);
                }
                else if (token.StartsWith(starttag + "expand|", StringComparison.Ordinal))
                {
                    text = entity.Expand(bag, text, replacepatterns, token);
                }
                else if (token.StartsWith(starttag + "iif|", StringComparison.Ordinal))
                {
                    text = entity.EvaluateIif(bag, text, scope, replacepatterns, token);
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
                    text = entity.Replace(bag, text, scope, replacepatterns, supressinvalidattributepaths, token);
                }

                token = text.GetNextToken(starttag);
            }

            // ReReplace curly things in the result to not rerun token replaces
            text = text.Replace(prevent_recursion_start, "{").Replace(prevent_recursion_end, "}");

            // Half-smart replacing those { and } handling just before returning the result
            text = text.Replace(special_chars_curly_start, "{").Replace(special_chars_curly_end, "}");

            if (sequence > 0)
            {
                text = ReplaceSequence(text, sequence);
            }

            bag.Logger.EndSection();
            return text;
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
            var child = token.GetSeparatedPart("|", 2);
            var relation = token.GetSeparatedPart("|", 3);
            var format = token.GetSeparatedPart("|", 4);
            var order = token.GetSeparatedPart("|", 5);
            var separator = token.GetSeparatedPart("|", 6).Replace("\\n", "\n").Replace("\\r", "\r");
            var distinct = token.GetSeparatedPart("|", 7).ToLowerInvariant();
            var activeonly = token.GetSeparatedPart("|", 8).ToLowerInvariant();
            var strMaxNumber = token.GetSeparatedPart("|", 9);
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
                var subvalue = Tokens(expanded, bag, format, nIndex, string.Empty, replacepatterns, false);
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

        private static string EvaluateIif(this Entity entity, IBag bag, string text, string scope, Dictionary<string, string> replacepatterns, string token)
        {
            bag.Logger.StartSection("EvaluateIif " + token);
            // <iif|value1|operator|value2|trueresult|falseresult>
            // value1, value2, trueresult and falseresult may be krulled
            // operator must be either of: eq neq lt gt le ge
            // Example: <iif|{firstname}|gt|Kalle|{firstname} är sent i alfabetet|{firstname} kommer tidigt>

            var value1 = token.GetSeparatedPart("|", 2);
            var oper = token.GetSeparatedPart("|", 3);
            var value2 = token.GetSeparatedPart("|", 4);
            var trueresult = token.GetSeparatedPart("|", 5);
            var falseresult = token.GetSeparatedPart("|", 6);
            value1 = entity.Tokens(bag, value1, 0, scope);
            value2 = entity.Tokens(bag, value2, 0, scope);
            decimal decValue1;
            bool numeric;
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

            decimal decValue2;
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

            var evaluation = false;
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
            var result = Tokens(entity, bag, evaluation ? trueresult : falseresult, 0, scope, replacepatterns, false);
            bag.Logger.EndSection();
            return text.Replace("<" + token + ">", result);
        }

        private static string Replace(this Entity entity, IBag bag, string text, string scope, Dictionary<string, string> replacepatterns, bool supressinvalidattributepaths, string token)
        {
            bag.Logger.StartSection("Replace " + token);

            var attributepath = token;
            if (token.ComparePositions(":", "<") < 0 &&    // Det finns kolon och det är inte en del av iif, expand etc (som börjar med < )
                token.ComparePositions(":", "|") < 0)      // Det finns kolon och det är inte en del av formatsträng (som börjar med | )
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
                    var tmp = attributepath.GetSeparatedPart(".", attributepos);
                    while (!string.IsNullOrWhiteSpace(tmp))
                    {
                        finalattribute = tmp; // "createdon"
                        attributepos++;
                        tmp = attributepath.GetSeparatedPart(".", attributepos);
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
                        else if (format == "<recordurl>")
                        {
                            if (deRef.Contains(finalattribute) && deRef[finalattribute] is EntityReference entref)
                            {
                                value = bag.Service.GetEntityFormUrl(entref);
                            }
                        }
                        else if (finalattribute.StartsWith("<expand|"))
                        {
                            value = deRef.Tokens(bag, finalattribute, 0, scope);
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
                // Replace curly things in the result to not rerun token replaces
                value = value.Replace("{", prevent_recursion_start).Replace("}", prevent_recursion_end);
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

        private static string ReplaceSequence(string text, int sequence)
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

        #endregion Private static methods
    }
}