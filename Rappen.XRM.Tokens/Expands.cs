using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;

namespace Rappen.XRM.Tokens
{
    internal static class Expands
    {
        internal static string Expand(this Entity entity, IBag bag, string text, string token)
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
                var subvalue = XRMTokens.Tokens(expanded, bag, format, nIndex, string.Empty, false, null);
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
    }
}