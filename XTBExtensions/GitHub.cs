using System;
using System.Diagnostics;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Rappen.XTB.Helpers.XTBExtensions
{
    public static class GitHub
    {
        public static void CreateNewIssueFromError(PluginControlBase tool, Exception error)
            => CreateNewIssue(tool, "```\n" + error.Message + "\n" + error.StackTrace + "\n```");

        public static void CreateNewIssue(PluginControlBase tool, string addedtext)
        {
            if (!(tool is IGitHubPlugin githubtool))
            {
                return;
            }
            var additionalInfo = "?body=[Write your comment/feedback/issue here]%0A%0A---%0A";
            additionalInfo += addedtext.Replace("\n", "%0A").Replace("&", "%26").Replace("   at ", "- ") + "%0A%0A---%0A";
            additionalInfo += $"-%20{tool.ProductName}%20Version:%20{Assembly.GetExecutingAssembly().GetName().Version}%0A";
            if (tool.ConnectionDetail != null)
            {
                additionalInfo += $"-%20DB%20Version:%20{tool.ConnectionDetail.OrganizationVersion}%0A";
                additionalInfo +=
                    $"-%20Deployment:%20{(tool.ConnectionDetail.WebApplicationUrl.ToLower().Contains("dynamics.com") ? "Online" : "OnPremise")}%0A";
            }
            var url = $"https://github.com/{githubtool.UserName}/{githubtool.RepositoryName}/issues/new";
            Process.Start(url + additionalInfo);
        }
    }
}
