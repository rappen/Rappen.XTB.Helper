using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;

namespace Rappen.XRM.Tokens
{
    internal static class Iifs
    {
        internal static string EvaluateIif(this Entity entity, IBag bag, string text, string scope, string token)
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
            value1 = XRMTokens.Tokens(entity, bag, value1, 0, scope, false, null);
            value2 = XRMTokens.Tokens(entity, bag, value2, 0, scope, false, null);
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
            var result = XRMTokens.Tokens(entity, bag, evaluation ? trueresult : falseresult, 0, scope, false, null);
            bag.Logger.EndSection();
            return text.Replace("<" + token + ">", result);
        }
    }
}