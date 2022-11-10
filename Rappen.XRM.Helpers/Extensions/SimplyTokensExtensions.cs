using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.Helpers.Extensions
{
    internal static class SimplyTokensExtensions
    {
        private const string prevent_recursion_start = "~~PRERECCURLYSTART~~";
        private const string prevent_recursion_end = "~~PRERECCURLYEND~~";

        internal static string SimplyTokens(this Entity entity, IBag bag, string text)
        {
            bag.Logger.StartSection("SimplyTokens");
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var token = text.GetNextSimplyToken("");
            while (!string.IsNullOrWhiteSpace(token))
            {
                bag.Logger.Log($"Found token: {token}");
                text = entity.Replace(bag, text, token);

                token = text.GetNextSimplyToken("");
            }

            // ReReplace curly things in the result to not rerun token replaces
            text = text.Replace(prevent_recursion_start, "{").Replace(prevent_recursion_end, "}");

            bag.Logger.EndSection();
            return text;
        }

        private static string Replace(this Entity entity, IBag bag, string text, string token)
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

            if (!string.IsNullOrEmpty(value))
            {
                // Replace curly things in the result to not rerun token replaces
                value = value.Replace("{", prevent_recursion_start).Replace("}", prevent_recursion_end);
            }

            // Only replace first (current) occurrence of ph, that is why we don't use string.Replace.
            token = string.Concat("{", token, "}");
            var phstart = Math.Max(text.IndexOf(token, StringComparison.InvariantCulture), 0);
            var phlength = token.Length;
            bag.Logger.Log($"Replacing {token} with {value}");
            text = text.Substring(0, phstart) + value + text.Substring(phstart + phlength);
            bag.Logger.Log($"Result: {text}");
            bag.Logger.EndSection();

            return text;
        }

        private static string GetNextSimplyToken(this string text, string scope)
        {
            string token;
            var startkrull = "{" + scope;
            if (text.Contains(startkrull))
            {
                token = text.GetFirstEnclosedPart("{", "", "}", scope);
            }
            else
            {
                token = "";
            }

            return token;
        }
    }
}