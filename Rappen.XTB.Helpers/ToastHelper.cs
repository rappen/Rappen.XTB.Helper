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
        public static void ToastIt(PluginControlBase plugin, string header, string text, string attribution = null, string logo = null, string image = null, string hero = null, params (string, string)[] buttons)
        {
            var toast = new ToastContentBuilder()
                .AddArgument("PluginControlId", plugin.PluginControlId.ToString())
                .AddArgument("action", "default")
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
            var imageurl = VerifyLocalLogoUri(image);
            if (imageurl != null)
            {
                toast.AddInlineImage(imageurl);
            }
            var herourl = VerifyLocalLogoUri(hero);
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
                    return remote;

                var localPath = Path.GetFullPath(Path.Combine(folder, fileName));

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