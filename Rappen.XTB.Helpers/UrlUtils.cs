using System;
using System.Diagnostics;
using System.Web;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers
{
    public static class UrlUtils
    {
        public static string UTM_MEDIUM = "XrmToolBox";
        public static string TOOL_NAME;
        public static string MVP_ID = "DX-MVP-5002475";

        public static void OpenUrl(object sender)
        {
            if (sender == null)
            {
                return;
            }
            var url = sender as string;
            if (string.IsNullOrWhiteSpace(url))
            {
                url = (sender as Control)?.Tag as string;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = (sender as LinkLabel)?.Tag as string;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = (sender as LinkLabel)?.Text;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = (sender as Control)?.Text;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }
            url = url.Trim();
            if (!url.StartsWith("http"))
            {
                return;
            }
            Process.Start(ProcessURL(url));
        }

        private static string ProcessURL(string url)
        {
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri _))
            {
                return url;
            }
            if (string.IsNullOrWhiteSpace(MVP_ID) || string.IsNullOrWhiteSpace(UTM_MEDIUM) || string.IsNullOrWhiteSpace(TOOL_NAME))
            {
                throw new Exception("MVP_ID, UTM_MEDIUM, TOOL_NAME must be set.");
            }
            var urib = new UriBuilder(url);
            var qry = HttpUtility.ParseQueryString(urib.Query);
            if (urib.Host.ToLowerInvariant().Contains("microsoft.com"))
            {
                qry.Add("WT.mc_id", MVP_ID);
                urib.Path = urib.Path.Replace("/en-us/", "/");
            }
            qry["utm_source"] = TOOL_NAME;
            qry["utm_medium"] = UTM_MEDIUM;
            urib.Query = qry.ToString();
            return urib.Uri.ToString();
        }
    }
}