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
        // Consistent with ExcelHelper.Try: best-effort operations without surfacing exceptions
        private static void Try(Action action) { try { action(); } catch { } }

        /// <summary>
        /// Displays a toast notification with customizable content, images, and action buttons for the specified
        /// plugin.
        /// </summary>
        /// <remarks>The notification content and images are subject to platform and system notification
        /// limitations. Image URIs must refer to valid local resources to be displayed. This method is typically used
        /// to provide user feedback or prompt actions within the context of the specified plugin. All images with http-url
        /// are cached locally use those when possibly.</remarks>
        /// <param name="plugin">The plugin control for which the toast notification is displayed. Cannot be null.</param>
        /// <param name="header">The header text to display in the toast notification. Cannot be null or empty.</param>
        /// <param name="text">The main body text of the toast notification. Cannot be null or empty.</param>
        /// <param name="attribution">Optional attribution text to display in the notification. If null or empty, no attribution is shown.</param>
        /// <param name="logo">Optional URI of the logo image to display in the notification.</param>
        /// <param name="image">Optional URI of an inline image to display in the notification. If null or invalid, no image is shown.</param>
        /// <param name="hero">Optional URI of a hero image to display prominently in the notification. If null or invalid, no hero image
        /// is shown.</param>
        /// <param name="buttons">An array of tuples representing action buttons to include in the notification. Each tuple contains the
        /// button label and the associated action argument. If no buttons are provided, the notification will not
        /// include any action buttons.</param>
        public static void ToastIt(
            PluginControlBase plugin,
            string sender,
            string header,
            string text,
            string attribution = null,
            string logo = null,
            string image = null,
            string hero = null,
            params (string, string)[] buttons)
        {
            var toast = new ToastContentBuilder()
                .AddArgument("PluginControlId", plugin.PluginControlId.ToString())
                .AddArgument("action", "default")
                .AddArgument("sender", sender)
                .SetToastDuration(ToastDuration.Long)
                .AddHeader(plugin.ToolName, header, InstallationInfo.Instance.InstallationId.ToString())
                .AddText(text);
            if (!string.IsNullOrEmpty(attribution))
            {
                toast.AddAttributionText(attribution);
            }
            var logourl = VerifyLocalImageUri(logo);
            if (logourl != null)
            {
                toast.AddAppLogoOverride(logourl);
            }
            var imageurl = VerifyLocalImageUri(image);
            if (imageurl != null)
            {
                toast.AddInlineImage(imageurl);
            }
            var herourl = VerifyLocalImageUri(hero);
            if (herourl != null)
            {
                toast.AddHeroImage(herourl);
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

        private static Uri VerifyLocalImageUri(string imageuri)
        {
            if (string.IsNullOrWhiteSpace(imageuri))
            {
                return null;
            }

            try
            {
                // Stage 1: expand environment variables (best-effort)
                var imageexpanded = imageuri;
                Try(() => imageexpanded = Environment.ExpandEnvironmentVariables(imageuri));

                // Stage 2: handle absolute URIs
                if (Uri.TryCreate(imageexpanded, UriKind.Absolute, out var uri))
                {
                    // Stage 2a: Absolute http/https
                    if (IsHttp(uri))
                    {
                        return CacheRemote(uri);
                    }
                    // Stage 2b: Absolute file:// and exists
                    if (uri.IsFile && File.Exists(uri.LocalPath))
                    {
                        return uri;
                    }
                }

                // Stage 3: treat as filesystem path (relative or unqualified)
                string imagefull = null;
                Try(() => imagefull = Path.GetFullPath(imageexpanded));
                if (!string.IsNullOrEmpty(imagefull) && File.Exists(imagefull))
                {
                    return new Uri(imagefull);
                }
            }
            catch { }

            return null;
        }

        private static bool IsHttp(Uri uri) => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

        private static Uri CacheRemote(Uri remote)
        {
            try
            {
                var folder = Path.Combine(Paths.PluginsPath, "ToastImages");
                Directory.CreateDirectory(folder);

                var fileName = Path.GetFileName(remote.LocalPath);
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
                {
                    return remote;
                }

                var localPath = Path.GetFullPath(Path.Combine(folder, fileName));

                // Redownload if missing, empty, or older than 24 hours
                if (!File.Exists(localPath) ||
                    new FileInfo(localPath).Length == 0 ||
                    (DateTime.UtcNow - File.GetLastWriteTimeUtc(localPath)) > TimeSpan.FromMinutes(24))
                {
                    // Ensure we don't reuse any stale content by deleting the existing file first
                    if (File.Exists(localPath))
                    {
                        Try(() => File.SetAttributes(localPath, FileAttributes.Normal));
                        Try(() => File.Delete(localPath));
                    }

                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
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