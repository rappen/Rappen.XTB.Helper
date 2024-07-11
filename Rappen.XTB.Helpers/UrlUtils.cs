using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Rappen.XTB.Helpers.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers
{
    public static class UrlUtils
    {
        public static string UTM_MEDIUM = "XrmToolBox";
        public static string TOOL_NAME;
        public static string MVP_ID = "DX-MVP-5002475";

        public static void OpenUrl(object sender, ConnectionDetail connectionDetail = null)
        {
            if (sender == null)
            {
                return;
            }
            var noextraparams = false;
            var url = GetUrl(sender);
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetUrl((sender as LinkLabel)?.Tag);
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetUrl((sender as LinkLabel)?.Text);
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetUrl((sender as ToolStripItem)?.Tag);
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetUrl((sender as ToolStripItem)?.Text);
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetUrl((sender as Control)?.Tag);
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetUrl((sender as Control)?.Text);
            }
            if (string.IsNullOrWhiteSpace(url) && sender is Entity entity && connectionDetail != null)
            {
                url = entity.GetEntityUrl(connectionDetail);
                noextraparams = !string.IsNullOrWhiteSpace(url);
            }
            if (string.IsNullOrWhiteSpace(url) && sender is EntityReference entityref && connectionDetail != null)
            {
                url = entityref.GetEntityUrl(connectionDetail);
                noextraparams = !string.IsNullOrWhiteSpace(url);
            }
            if (string.IsNullOrEmpty(url) && sender is XRMRecordEventArgs xrmenvarg && connectionDetail != null)
            {
                url = xrmenvarg.Entity?.GetEntityUrl(connectionDetail);
                noextraparams = !string.IsNullOrWhiteSpace(url);
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }
            if (!noextraparams && url.Contains(".dynamics.com/"))
            {
                noextraparams = true;
            }
            if (!noextraparams)
            {
                url = ProcessURL(url.Trim());
            }
            if (connectionDetail != null)
            {
                connectionDetail.OpenUrlWithBrowserProfile(new Uri(url));
            }
            else
            {
                Process.Start(url);
            }
        }

        public static string GetFullWebApplicationUrl(ConnectionDetail ConnectionDetail)
        {
            var url = ConnectionDetail.WebApplicationUrl;
            if (string.IsNullOrEmpty(url))
            {
                url = ConnectionDetail.ServerName;
            }
            if (!url.ToLower().StartsWith("http"))
            {
                url = string.Concat("http://", url);
            }
            var uri = new Uri(url);
            if (!uri.Host.EndsWith(".dynamics.com"))
            {
                if (string.IsNullOrEmpty(uri.AbsolutePath.Trim('/')))
                {
                    uri = new Uri(uri, ConnectionDetail.Organization);
                }
            }
            return uri.ToString();
        }

        public static string GetEntityUrl(this Entity entity, ConnectionDetail ConnectionDetail)
        {
            var entref = entity.ToEntityReference();
            switch (entref.LogicalName)
            {
                case "activitypointer":
                    if (!entity.Contains("activitytypecode"))
                    {
                        MessageBox.Show("To open records of type activitypointer, attribute 'activitytypecode' must be included in the query.", "Open Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        entref.LogicalName = string.Empty;
                    }
                    else
                    {
                        entref.LogicalName = entity["activitytypecode"].ToString();
                    }
                    break;

                case "activityparty":
                    if (!entity.Contains("partyid"))
                    {
                        MessageBox.Show("To open records of type activityparty, attribute 'partyid' must be included in the query.", "Open Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        entref.LogicalName = string.Empty;
                    }
                    else
                    {
                        var party = (EntityReference)entity["partyid"];
                        entref.LogicalName = party.LogicalName;
                        entref.Id = party.Id;
                    }
                    break;
            }
            return entref.GetEntityUrl(ConnectionDetail);
        }

        public static string GetEntityUrl(this EntityReference entref, ConnectionDetail ConnectionDetail)
        {
            if (!string.IsNullOrEmpty(entref.LogicalName) && !entref.Id.Equals(Guid.Empty))
            {
                var url = GetFullWebApplicationUrl(ConnectionDetail);
                url = string.Concat(url,
                    url.EndsWith("/") ? "" : "/",
                    "main.aspx?etn=",
                    entref.LogicalName,
                    "&pagetype=entityrecord&id=",
                    entref.Id.ToString());
                return url;
            }
            return string.Empty;
        }

        public static T DownloadXml<T>(this Uri uri, T defaultvalue = default(T))
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var webRequestXml = HttpWebRequest.Create(uri) as HttpWebRequest;
            webRequestXml.Accept = "text/html, application/xhtml+xml, */*";
            try
            {
                using (var response = webRequestXml.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    var strContent = reader.ReadToEnd();
                    return (T)XmlSerializerHelper.Deserialize(strContent, typeof(T));
                }
            }
            catch
            {
                return defaultvalue;
            }
        }

        private static string GetUrl(object holder)
        {
            if (holder is string url && url.Trim().StartsWith("http"))
            {
                return url.Trim();
            }
            return string.Empty;
        }

        private static string ProcessURL(string url)
        {
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri _))
            {
                return url;
            }
            if (string.IsNullOrWhiteSpace(UTM_MEDIUM) || string.IsNullOrWhiteSpace(TOOL_NAME))
            {
                throw new Exception($"UTM_MEDIUM='{UTM_MEDIUM}', TOOL_NAME='{TOOL_NAME}' must be set.");
            }
            var urib = new UriBuilder(url);
            var qry = HttpUtility.ParseQueryString(urib.Query);
            if (urib.Host.ToLowerInvariant().Contains("microsoft.com"))
            {
                if (string.IsNullOrWhiteSpace(MVP_ID))
                {
                    throw new Exception($"MVP_ID must be set.");
                }
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