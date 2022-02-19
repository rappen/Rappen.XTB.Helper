using Rappen.XTB.Helpers.Extensions;
using System;
using System.Diagnostics;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Rappen.XTB.Helpers.XTBExtensions
{
    public static class GitHub
    {
        public static void CreateNewIssueFromError(PluginControlBase tool, Exception error, string moreinfo)
            => CreateNewIssue(tool, "```\n" + error.ToTypeString() + ":\n" + error.Message + "\n" + error.StackTrace + "\n```", moreinfo);

        public static void CreateNewIssue(PluginControlBase tool, string addedtext, string extrainfo)
        {
            if (!(tool is IGitHubPlugin githubtool))
            {
                return;
            }
            var additionalInfo = "?body=[Write any error info to resolve easier]\n\n---\n";
            additionalInfo += addedtext.Replace("   at ", "- ") + "\n\n---\n";
            if (!string.IsNullOrWhiteSpace(extrainfo))
            {
                additionalInfo += "\n```\n" + extrainfo + "\n```\n---\n";
            }
            additionalInfo += $"- {tool.ProductName} Version: {Assembly.GetExecutingAssembly().GetName().Version}\n";
            if (tool.ConnectionDetail != null)
            {
                additionalInfo += $"- DB Version: {tool.ConnectionDetail.OrganizationVersion}\n";
                additionalInfo += $"- Deployment: {(tool.ConnectionDetail.WebApplicationUrl.ToLower().Contains("dynamics.com") ? "Online" : "OnPremise")}\n";
            }
            additionalInfo = additionalInfo.Replace("\n", "%0A").Replace("&", "%26").Replace(" ", "%20");
            var url = $"https://github.com/{githubtool.UserName}/{githubtool.RepositoryName}/issues/new";
            Process.Start(url + additionalInfo);
        }
    }
}
