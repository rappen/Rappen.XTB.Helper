using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Globalization;

namespace Rappen.XRM.Tokens
{
    internal static class Systems
    {
        internal static string ReplaceSystem(IBag bag, string text, string token)
        {
            bag.Logger.StartSection("ReplaceSystem " + token);
            string systemtoken = token.GetSeparatedPart("|", 2).ToLowerInvariant();
            string format = token.GetSeparatedPart("|", 3);
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
                value = XRMTokens.Tokens(user, bag, token, 0, string.Empty, false, null);
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
    }
}