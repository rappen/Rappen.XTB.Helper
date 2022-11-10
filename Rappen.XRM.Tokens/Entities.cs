using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Rappen.XRM.Tokens
{
    internal static class Entities
    {
        private static Dictionary<string, string> xmlReplacePatterns = new Dictionary<string, string>() { { "&", "&amp;" }, { "<", "&lt;" }, { ">", "&gt;" }, { "\"", "&quot;" }, { "'", "&apos;" } };
        private const string prevent_recursion_start = "~~PRERECCURLYSTART~~";
        private const string prevent_recursion_end = "~~PRERECCURLYEND~~";

        internal static string ReplaceEntityTokens(this Entity entity, IBag bag, string text, string scope, Dictionary<string, string> replacepatterns, bool supressinvalidattributepaths, string token)
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
                            value = XRMTokens.Tokens(deRef, bag, finalattribute, 0, scope, false, null);
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
    }
}