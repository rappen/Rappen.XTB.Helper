using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using System.Net;
using XrmToolBox.AppCode.AppInsights;
using XrmToolBox.Extensibility;

namespace Rappen.XTB.Helpers
{
    public static class ToastHelper
    {
        public static void ToastIt(PluginControlBase plugin, string header, string text, string attribution = null, string logo = null, params (string, string)[] buttons)
        {
            var toast = new ToastContentBuilder()
                .AddArgument("PluginControlId", plugin.PluginControlId.ToString())
                .AddHeader(plugin.ToolName, header, InstallationInfo.Instance.InstallationId.ToString())
                .AddText(text);
            if (!string.IsNullOrEmpty(attribution))
            {
                toast.AddAttributionText(attribution);
            }
            var logourl = VerifyLocalLogoUri(logo);
            if (logourl != null)
            {
                toast.AddAppLogoOverride(logourl);
            }
            foreach (var button in buttons ?? Array.Empty<(string, string)>())
            {
                toast.AddButton(new ToastButton()
                    .SetContent(button.Item1)
                    .AddArgument("action", button.Item2)
                    .SetBackgroundActivation());
            }
            toast.Show();
        }

        private static Uri VerifyLocalLogoUri(string logourl)
        {
            if (string.IsNullOrWhiteSpace(logourl))
                return null;

            var expanded = Environment.ExpandEnvironmentVariables(logourl);

            // Absolute URI?
            if (Uri.TryCreate(expanded, UriKind.Absolute, out var uri))
            {
                if (uri.IsFile && File.Exists(uri.LocalPath))
                    return uri;

                if (IsHttp(uri))
                    return CacheRemote(uri);
            }

            // Try as filesystem path (relative or unqualified)
            try
            {
                var full = Path.GetFullPath(expanded);
                if (File.Exists(full))
                    return new Uri(full);
            }
            catch { /* ignore */ }

            // Last attempt: treat as http/https if it parses now
            if (Uri.TryCreate(expanded, UriKind.Absolute, out uri) && IsHttp(uri))
                return CacheRemote(uri);

            return null;
        }

        private static bool IsHttp(Uri uri) =>
            uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

        private static Uri CacheRemote(Uri remote)
        {
            try
            {
                var folder = Path.Combine(Paths.PluginsPath, "ToastImages");
                Directory.CreateDirectory(folder);

                var fileName = Path.GetFileName(remote.LocalPath);
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
                    fileName = "ToastLogo.png";

                var localPath = Path.Combine(folder, fileName);

                if (!File.Exists(localPath) || new FileInfo(localPath).Length == 0)
                {
                    ServicePointManager.SecurityProtocol |=
                        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    using (var wc = new WebClient())
                    {
                        wc.DownloadFile(remote, localPath);
                    }
                }

                return new Uri(localPath); // Prefer file:// for toast reliability
            }
            catch
            {
                return remote; // Fallback to remote if caching fails
            }
        }
    }
}