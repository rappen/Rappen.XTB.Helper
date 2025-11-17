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
        /// <remarks>
        /// The notification content and images are subject to platform and system notification limitations.
        /// Image URIs must refer to valid local resources to be displayed. This method is typically used to
        /// provide user feedback or prompt actions within the context of the specified plugin. All images with
        /// http/https URIs are cached locally and used when possible.
        /// </remarks>
        /// <param name="plugin">The plugin control for which the toast notification is displayed. Cannot be null.</param>
        /// <param name="sender">A logical sender identifier used as an activation argument for routing.</param>
        /// <param name="header">The header text to display in the toast notification. Cannot be null or empty.</param>
        /// <param name="text">The main body text of the toast notification. Cannot be null or empty.</param>
        /// <param name="attribution">Optional attribution text to display in the notification. If null or empty, no attribution is shown.</param>
        /// <param name="logo">Optional URI online or locally of the logo image to display in the notification.</param>
        /// <param name="image">Optional URI of online or locally an inline image to display in the notification. If null or invalid, no image is shown.</param>
        /// <param name="hero">Optional URI online or locally of a hero image to display prominently in the notification. If null or invalid, no hero image is shown.</param>
        /// <param name="buttons">
        /// An array of tuples representing action buttons to include in the notification. Each tuple contains the
        /// button label and the associated action argument. If no buttons are provided, the notification will not
        /// include any action buttons.
        /// </param>
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
            if (plugin == null || string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            ToastContentBuilder toast = new ToastContentBuilder()
                .AddArgument("PluginControlId", plugin.PluginControlId.ToString())
                .AddArgument("action", "default")
                .AddArgument("sender", sender)
                .SetToastDuration(ToastDuration.Long)
                .AddHeader(plugin.ToolName, header, InstallationInfo.Instance.InstallationId.ToString())
                .AddText(text);

            if (!string.IsNullOrEmpty(attribution))
            {
                Try(() => toast.AddAttributionText(attribution));
            }
            if (VerifyLocalImageUri(logo) is Uri logoUri)
            {
                Try(() => toast.AddAppLogoOverride(logoUri));
            }
            if (VerifyLocalImageUri(image) is Uri imageUri)
            {
                Try(() => toast.AddInlineImage(imageUri));
            }
            if (VerifyLocalImageUri(hero) is Uri heroUri)
            {
                Try(() => toast.AddHeroImage(heroUri));
            }

            foreach ((string, string) button in buttons ?? Array.Empty<(string, string)>())
            {
                Try(() => toast.AddButton(new ToastButton()
                    .SetContent(button.Item1)
                    .AddArgument("action", button.Item2)
                    .SetBackgroundActivation()));
            }

            Try(toast.Show);
        }

        private static Uri VerifyLocalImageUri(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // Expand env vars (ignore errors)
            string expanded = value;
            Try(() => expanded = Environment.ExpandEnvironmentVariables(value));

            // Absolute URI
            if (Uri.TryCreate(expanded, UriKind.Absolute, out Uri uri))
            {
                if (uri.IsFile && File.Exists(uri.LocalPath))
                {
                    return uri;
                }
                if (IsHttp(uri))
                {
                    return CacheRemote(uri);
                }
            }

            // Treat as local path
            string full = null;
            Try(() => full = Path.GetFullPath(expanded));
            if (!string.IsNullOrEmpty(full) && File.Exists(full))
            {
                return new Uri(full);
            }

            return null;
        }

        private static bool IsHttp(Uri uri) => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

        private static Uri CacheRemote(Uri remote)
        {
            try
            {
                string folder = Path.Combine(Paths.PluginsPath, "ToastImages");
                Directory.CreateDirectory(folder);

                string fileName = Path.GetFileName(remote.LocalPath);
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
                {
                    return remote;
                }

                string localPath = Path.Combine(folder, fileName);
                if (!File.Exists(localPath) ||
                    new FileInfo(localPath).Length == 0 ||
                    (DateTime.UtcNow - File.GetLastWriteTimeUtc(localPath)) > TimeSpan.FromHours(24))
                {
                    // Delete old file first (ignore errors)
                    if (File.Exists(localPath))
                    {
                        Try(() => File.SetAttributes(localPath, FileAttributes.Normal));
                        Try(() => File.Delete(localPath));
                    }

                    ServicePointManager.SecurityProtocol |=
                        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    using (var wc = new WebClient())
                    {
                        wc.DownloadFile(remote, localPath);
                    }
                }

                return new Uri(Path.GetFullPath(localPath));
            }
            catch
            {
                // Ignored: image cache is optional; fall back to remote URI if caching fails
                return remote;
            }
        }
    }
}