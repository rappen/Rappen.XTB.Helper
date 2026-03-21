using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rappen.XTB.Helpers
{
    /// <summary>
    /// Reads a text file from an online location with a DEBUG-only local fallback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Behavior:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// In <c>DEBUG</c>, if <paramref name="localFolder"/> is provided and the local file exists, the local file is read instead of downloading.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// In <c>DEBUG</c>, if the local file does not exist, the content is downloaded and then cached locally (best effort; caching errors are ignored).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// In non-<c>DEBUG</c> builds, local fallback and caching code is not included; the file is always downloaded.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// The API returns text as UTF-8 with BOM detection. Network/IO errors return <see cref="string.Empty"/> or <see langword="null"/>
    /// depending on <paramref name="alwaysReturns"/>.
    /// </para>
    /// </remarks>
    public static class OnlineFile
    {
        /// <summary>
        /// Downloads a text file by reading it locally (DEBUG-only) or downloading it from <paramref name="baseUri"/>.
        /// </summary>
        /// <param name="baseUri">Base URL (absolute) to combine with <paramref name="fileName"/>.</param>
        /// <param name="fileName">File name or relative path to append to <paramref name="baseUri"/>.</param>
        /// <param name="localFolder">
        /// In <c>DEBUG</c>, optional local folder used for fallback reading and caching. In non-<c>DEBUG</c>, ignored.
        /// </param>
        /// <param name="alwaysReturns">
        /// If <see langword="true"/>, returns <see cref="string.Empty"/> when failing to get/validate content.
        /// If <see langword="false"/>, returns <see langword="null"/> when failing to get/validate content.
        /// </param>
        /// <param name="timeoutMilliseconds">HTTP request timeout in milliseconds.</param>
        /// <param name="validator">
        /// Optional content validator. If specified and returns <see langword="false"/>, the content is treated as invalid and the method returns
        /// <see cref="string.Empty"/> or <see langword="null"/> depending on <paramref name="alwaysReturns"/>.
        /// </param>
        /// <returns>The file content, or an empty/null fallback depending on <paramref name="alwaysReturns"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseUri"/> or <paramref name="fileName"/> is null/whitespace.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="baseUri"/> is not a well-formed absolute URI.</exception>
        public static string DownloadText(
            string baseUri,
            string fileName,
            string localFolder = null,
            bool alwaysReturns = true,
            int timeoutMilliseconds = 15000,
            Func<string, bool> validator = null)
        {
            var text = DownloadTextCore(baseUri, fileName, localFolder, timeoutMilliseconds);
            if (text == null)
            {
                return alwaysReturns ? string.Empty : null;
            }

            if (validator != null && !validator(text))
            {
                return alwaysReturns ? string.Empty : null;
            }

            return text;
        }

        /// <summary>
        /// Async wrapper for <see cref="DownloadText"/>. When <paramref name="runAsync"/> is true this uses <see cref="Task.Run(Action)"/>,

        /// not true async I/O.
        /// </summary>
        /// <param name="baseUri">Base URL (absolute) to combine with <paramref name="fileName"/>.</param>
        /// <param name="fileName">File name or relative path to append to <paramref name="baseUri"/>.</param>
        /// <param name="localFolder">
        /// In <c>DEBUG</c>, optional local folder used for fallback reading and caching. In non-<c>DEBUG</c>, ignored.
        /// </param>
        /// <param name="alwaysReturns">
        /// If <see langword="true"/>, returns <see cref="string.Empty"/> when failing to get/validate content.
        /// If <see langword="false"/>, returns <see langword="null"/> when failing to get/validate content.
        /// </param>
        /// <param name="runAsync">If false, runs synchronously and returns a completed task.</param>
        /// <param name="timeoutMilliseconds">HTTP request timeout in milliseconds.</param>
        /// <param name="validator">Optional content validator.</param>
        /// <param name="cancellationToken">
        /// Cancellation token. Note: cancellation is checked before executing; it does not cancel an in-flight synchronous HTTP request.
        /// </param>
        /// <returns>A task producing the file content, or an empty/null fallback depending on <paramref name="alwaysReturns"/>.</returns>
        public static Task<string> DownloadTextAsync(
            string baseUri,
            string fileName,
            string localFolder = null,
            bool alwaysReturns = true,
            bool runAsync = true,
            int timeoutMilliseconds = 15000,
            Func<string, bool> validator = null,
            CancellationToken cancellationToken = default)
        {
            if (!runAsync)
            {
                return Task.FromResult(DownloadText(baseUri, fileName, localFolder, alwaysReturns, timeoutMilliseconds, validator));
            }

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DownloadText(baseUri, fileName, localFolder, alwaysReturns, timeoutMilliseconds, validator);
            }, cancellationToken);
        }

        public static string GetTextFromMaybeUrl(string text, string localDir)
        {
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                try
                {
                    var fileName = Path.GetFileName(uri.AbsolutePath);
                    var dir = uri.AbsolutePath.Substring(0, uri.AbsolutePath.Length - fileName.Length);
                    var baseUri = uri.GetLeftPart(UriPartial.Authority) + dir;

                    return OnlineFile.DownloadText(baseUri, fileName, localDir);
                }
                catch { }
            }
            return text;
        }

        /// <summary>
        /// Core implementation: returns downloaded/local text, or <see langword="null"/> on any failure.
        /// </summary>
        private static string DownloadTextCore(
            string baseUri,
            string fileName,
            string localFolder,
            int timeoutMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(baseUri))
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var baseUriObj))
            {
                throw new ArgumentException("Base URI must be a well-formed absolute URI.", nameof(baseUri));
            }

            string localPath = null;

#if DEBUG
            if (!string.IsNullOrWhiteSpace(localFolder))
            {
                try
                {
                    localPath = Path.Combine(localFolder, fileName);
                    if (File.Exists(localPath))
                    {
                        return File.ReadAllText(localPath, Encoding.UTF8);
                    }
                }
                catch
                {
                    // Ignore and fall back to remote fetch
                }
            }
#endif

            var remoteUri = new Uri(baseUriObj, fileName);

            // Keep compatibility with .NET Framework 4.8 when default TLS settings may vary.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(remoteUri);
            request.Accept = "text/plain, text/markdown, application/xhtml+xml, text/html, */*";
            request.Timeout = timeoutMilliseconds;

            try
            {
                using var response = request.GetResponse();
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var text = reader.ReadToEnd();

#if DEBUG
                if (!string.IsNullOrWhiteSpace(localFolder) &&
                    !string.IsNullOrWhiteSpace(localPath) &&
                    !File.Exists(localPath))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(localPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.WriteAllText(localPath, text ?? string.Empty, Encoding.UTF8);
                    }
                    catch
                    {
                        // Swallow caching errors in DEBUG mode
                    }
                }
#endif

                return text;
            }
            catch
            {
                return null;
            }
        }
    }
}