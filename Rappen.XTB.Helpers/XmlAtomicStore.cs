using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Rappen.XTB.Helpers
{
    /// <summary>
    /// Safe XML storage utilities:
    /// - Atomic, durable writes (temp + replace, optional write-through)
    /// - In-proc per-path locking + optional cross-proc named mutex
    /// - Symmetric, lock-coordinated file deserialization
    /// - XML string/stream helpers
    /// - Remote XML download + deserialize (with DEBUG local fallback)
    /// </summary>
    public static class XmlAtomicStore
    {
        #region Fields (locking)

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks =
            new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        #endregion Fields (locking)

        #region Serialization - Public API (Minimal overloads)

        /// <summary>
        /// Serializes the specified object to a file at the given path.
        /// </summary>
        /// <remarks>This method ensures that the serialization process is thread-safe and writes the data
        /// immediately to the file system.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="instance">The object instance to serialize. Cannot be <see langword="null"/>.</param>
        /// <param name="path">The file path where the serialized object will be written. Cannot be <see langword="null"/> or empty.</param>
        public static void Serialize<T>(T instance, string path) =>
            Serialize(instance, path, writeThrough: true, useNamedMutex: true, CancellationToken.None);

        /// <summary>
        /// Asynchronously serializes the specified object to a file at the given path.
        /// </summary>
        /// <remarks>This method performs the serialization operation asynchronously and supports
        /// additional options such as write-through and named mutex usage.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="instance">The object instance to serialize. Cannot be <see langword="null"/>.</param>
        /// <param name="path">The file path where the serialized data will be written. Cannot be <see langword="null"/> or empty.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        public static Task SerializeAsync<T>(T instance, string path) =>
            SerializeAsync(instance, path, runAsync: true, writeThrough: true, useNamedMutex: true, CancellationToken.None);

        #endregion Serialization - Public API (Minimal overloads)

        #region Serialization - Public API (Advanced overloads with CancellationToken)

        /// <summary>
        /// Serializes the specified object to the file at the given path.
        /// </summary>
        /// <remarks>This method provides advanced options for serialization, including control over file
        /// write behavior and synchronization. Ensure the <paramref name="path"/> is accessible and the caller has
        /// appropriate permissions.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="instance">The object instance to serialize. Cannot be <see langword="null"/>.</param>
        /// <param name="path">The file path where the serialized data will be written. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="writeThrough">A value indicating whether to ensure the data is immediately written to disk. If <see langword="true"/>, the
        /// method ensures the data is flushed to disk; otherwise, it may be buffered.</param>
        /// <param name="useNamedMutex">A value indicating whether to use a named mutex to synchronize access to the file. If <see
        /// langword="true"/>, a named mutex is used to prevent concurrent writes; otherwise, no mutex is used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation can be canceled by passing a token in the
        /// canceled state.</param>
        public static void Serialize<T>(
            T instance,
            string path,
            bool writeThrough,
            bool useNamedMutex,
            CancellationToken cancellationToken) =>
            SerializeInternal(instance, path, writeThrough, useNamedMutex, cancellationToken);

        /// <summary>
        /// Serializes the specified object to a file at the given path, with options for asynchronous execution and
        /// concurrency control.
        /// </summary>
        /// <remarks>This method provides flexibility for both synchronous and asynchronous serialization
        /// scenarios. When <paramref name="runAsync"/> is <see langword="true"/>, the serialization is performed on a
        /// background thread, allowing the calling thread to continue execution. The <paramref name="useNamedMutex"/>
        /// parameter ensures safe access to the file in multi-process environments.</remarks>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="instance">The object instance to serialize. Cannot be <see langword="null"/>.</param>
        /// <param name="path">The file path where the serialized data will be written. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="runAsync">A value indicating whether the serialization should be performed asynchronously. If <see langword="true"/>,
        /// the operation runs on a background thread; otherwise, it runs synchronously.</param>
        /// <param name="writeThrough">A value indicating whether the file system should flush the data immediately after writing, ensuring
        /// durability.</param>
        /// <param name="useNamedMutex">A value indicating whether a named mutex should be used to synchronize access to the file, preventing
        /// concurrent writes from multiple processes.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If the token is canceled, the operation is aborted.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. If <paramref name="runAsync"/> is <see
        /// langword="false"/>, the returned task is already completed.</returns>
        public static Task SerializeAsync<T>(
            T instance,
            string path,
            bool runAsync,
            bool writeThrough,
            bool useNamedMutex,
            CancellationToken cancellationToken)
        {
            if (!runAsync)
            {
                SerializeInternal(instance, path, writeThrough, useNamedMutex, cancellationToken);
                return Task.CompletedTask;
            }
            return Task.Run(() => SerializeInternal(instance, path, writeThrough, useNamedMutex, cancellationToken), cancellationToken);
        }

        #endregion Serialization - Public API (Advanced overloads with CancellationToken)

        #region Deserialization - Public API (Minimal overloads)

        /// <summary>
        /// Deserializes an object of the specified type from the file at the given path.
        /// </summary>
        /// <remarks>This method uses a named mutex to ensure thread safety during
        /// deserialization.</remarks>
        /// <typeparam name="T">The type of the object to deserialize. The type must have a parameterless constructor.</typeparam>
        /// <param name="path">The file path from which to deserialize the object. The path must not be null or empty.</param>
        /// <param name="alwaysReturns">A value indicating whether the method should always return an instance of the specified type. If <see
        /// langword="true"/>, a new instance of <typeparamref name="T"/> is returned if deserialization fails.</param>
        /// <returns>An instance of the specified type <typeparamref name="T"/> deserialized from the file. If deserialization
        /// fails and <paramref name="alwaysReturns"/> is <see langword="true"/>, a new instance of <typeparamref
        /// name="T"/> is returned.</returns>
        public static T Deserialize<T>(string path, bool alwaysReturns = true) where T : new() =>
            DeserializeInternal<T>(path, alwaysReturns, useNamedMutex: true, CancellationToken.None);

        /// <summary>
        /// Asynchronously deserializes an object of type <typeparamref name="T"/> from the specified file path.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize. The type must have a parameterless constructor.</typeparam>
        /// <param name="path">The file path from which the object will be deserialized. The path must point to a valid file.</param>
        /// <param name="alwaysReturns">A boolean value indicating whether the method should always return a result, even in cases where the file
        /// content is invalid. If <see langword="true"/>, a default instance of <typeparamref name="T"/> will be
        /// returned in such cases.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object of type
        /// <typeparamref name="T"/>.</returns>
        public static Task<T> DeserializeAsync<T>(string path, bool alwaysReturns = true) where T : new() =>
            DeserializeAsync<T>(path, alwaysReturns, runAsync: true, useNamedMutex: true, CancellationToken.None);

        #endregion Deserialization - Public API (Minimal overloads)

        #region Deserialization - Public API (Advanced overloads with CancellationToken)

        /// <summary>
        /// Deserializes an object of the specified type from the file at the given path.
        /// </summary>
        /// <remarks>The method uses advanced overloads to support cancellation and optional
        /// synchronization mechanisms. Ensure the file at the specified path exists and contains valid serialized data
        /// for the specified type.</remarks>
        /// <typeparam name="T">The type of the object to deserialize. The type must have a parameterless constructor.</typeparam>
        /// <param name="path">The path to the file containing the serialized object data.</param>
        /// <param name="alwaysReturns">A value indicating whether the method guarantees a return value, even in certain failure scenarios.</param>
        /// <param name="useNamedMutex">A value indicating whether a named mutex should be used to synchronize access to the file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests, which can be used to cancel the deserialization operation.</param>
        /// <returns>An instance of the specified type <typeparamref name="T"/> populated with the deserialized data.</returns>
        public static T Deserialize<T>(
            string path,
            bool alwaysReturns,
            bool useNamedMutex,
            CancellationToken cancellationToken) where T : new() =>
            DeserializeInternal<T>(path, alwaysReturns, useNamedMutex, cancellationToken);

        /// <summary>
        /// Deserializes an object of the specified type from the file at the given path.
        /// </summary>
        /// <remarks>If <paramref name="runAsync"/> is <see langword="false"/>, the operation is performed
        /// synchronously and the result is returned immediately. If <paramref name="alwaysReturns"/> is <see
        /// langword="true"/>, the method guarantees a non-null result by returning a default instance of <typeparamref
        /// name="T"/> if deserialization fails.</remarks>
        /// <typeparam name="T">The type of the object to deserialize. The type must have a parameterless constructor.</typeparam>
        /// <param name="path">The path to the file containing the serialized object.</param>
        /// <param name="alwaysReturns">A value indicating whether the method should always return a default instance of <typeparamref name="T"/> if
        /// deserialization fails.</param>
        /// <param name="runAsync">A value indicating whether the deserialization should be performed asynchronously.</param>
        /// <param name="useNamedMutex">A value indicating whether a named mutex should be used to synchronize access to the file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object of type
        /// <typeparamref name="T"/>.</returns>
        public static Task<T> DeserializeAsync<T>(
            string path,
            bool alwaysReturns,
            bool runAsync,
            bool useNamedMutex,
            CancellationToken cancellationToken) where T : new()
        {
            if (!runAsync)
            {
                return Task.FromResult(DeserializeInternal<T>(path, alwaysReturns, useNamedMutex, cancellationToken));
            }
            return Task.Run(() => DeserializeInternal<T>(path, alwaysReturns, useNamedMutex, cancellationToken), cancellationToken);
        }

        #endregion Deserialization - Public API (Advanced overloads with CancellationToken)

        #region XML String / Stream Helpers

        /// <summary>
        /// Deserializes an XML string into an object of the specified type.
        /// </summary>
        /// <remarks>This method uses the <see cref="System.Xml.Serialization.XmlSerializer"/> to perform
        /// the deserialization.  If the XML string is invalid or deserialization fails, the method does not throw an
        /// exception but instead  returns a default value or a new instance of <typeparamref name="T"/> based on the
        /// <paramref name="alwaysReturns"/> parameter.</remarks>
        /// <typeparam name="T">The type of the object to deserialize. The type must have a parameterless constructor.</typeparam>
        /// <param name="xml">The XML string to deserialize. If the string is null, empty, or consists only of whitespace,  the method
        /// returns a default value or a new instance of <typeparamref name="T"/> based on the  <paramref
        /// name="alwaysReturns"/> parameter.</param>
        /// <param name="alwaysReturns">A boolean value indicating whether the method should always return a new instance of  <typeparamref
        /// name="T"/> when deserialization fails or the input is invalid. If <see langword="true"/>,  a new instance of
        /// <typeparamref name="T"/> is returned; otherwise, the default value of <typeparamref name="T"/>  is returned.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the provided XML string. If deserialization
        /// fails or the input is invalid, the return value depends on the <paramref name="alwaysReturns"/> parameter.</returns>
        public static T DeserializeFromString<T>(string xml, bool alwaysReturns) where T : new()
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return alwaysReturns ? new T() : default(T);
            }
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using var sr = new StringReader(xml);
                var obj = serializer.Deserialize(sr);
                return (T)obj;
            }
            catch
            {
                return alwaysReturns ? new T() : default(T);
            }
        }

        /// <summary>
        /// Deserializes an object of the specified type from the provided stream.
        /// </summary>
        /// <remarks>If the deserialization process encounters an error, the method will either return a
        /// new instance of <typeparamref name="T"/> or <see langword="default"/> depending on the value of <paramref
        /// name="alwaysReturns"/>.</remarks>
        /// <typeparam name="T">The type of the object to deserialize. The type must have a parameterless constructor.</typeparam>
        /// <param name="stream">The <see cref="Stream"/> containing the XML data to deserialize. Cannot be <see langword="null"/>.</param>
        /// <param name="alwaysReturns">A value indicating whether to return a new instance of <typeparamref name="T"/> if deserialization fails. If
        /// <see langword="true"/>, a new instance of <typeparamref name="T"/> is returned on failure; otherwise, <see
        /// langword="default"/> is returned.</param>
        /// <returns>An instance of <typeparamref name="T"/> deserialized from the stream, or a fallback value based on <paramref
        /// name="alwaysReturns"/> if deserialization fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
        public static T DeserializeFromStream<T>(Stream stream, bool alwaysReturns) where T : new()
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                var obj = serializer.Deserialize(stream);
                return (T)obj;
            }
            catch
            {
                return alwaysReturns ? new T() : default(T);
            }
        }

        #endregion XML String / Stream Helpers

        #region Remote XML - Download + Deserialize (with local DEBUG fallback)

        /// <summary>
        /// Downloads and deserializes an XML file from a remote URI into an object of the specified type. Optionally falls back
        /// to a local file in DEBUG mode and caches the remote XML locally.
        /// </summary>
        /// <remarks>In DEBUG mode, the method attempts to load the XML from a local file if <paramref
        /// name="localFolder"/> is provided. If the local file does not exist, the method fetches the XML from the remote
        /// server and optionally caches it locally. In non-DEBUG mode, the method always fetches the XML from the remote
        /// server.</remarks>
        /// <typeparam name="T">The type of the object to deserialize the XML into. Must have a parameterless constructor.</typeparam>
        /// <param name="baseUri">The base URI of the remote server. Must be a well-formed absolute URI.</param>
        /// <param name="fileName">The name of the XML file to download from the remote server.</param>
        /// <param name="localFolder">An optional local folder path to use for fallback in DEBUG mode and for caching the downloaded XML. If not provided,
        /// no local fallback or caching will occur. A good example to send in XrmToolBox: Paths.SettingsPath</param>
        /// <param name="alwaysReturns">A value indicating whether the method should return a new instance of <typeparamref name="T"/> if deserialization
        /// fails. If <see langword="true"/>, a new instance is returned; otherwise, <see langword="default"/> is returned.</param>
        /// <param name="timeoutMilliseconds">The maximum time, in milliseconds, to wait for the remote server to respond. Defaults to 15,000 milliseconds.</param>
        /// <returns>An instance of <typeparamref name="T"/> deserialized from the XML file. If the XML cannot be downloaded or
        /// deserialized, the return value depends on the <paramref name="alwaysReturns"/> parameter.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseUri"/> or <paramref name="fileName"/> is <see langword="null"/> or whitespace.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="baseUri"/> is not a well-formed absolute URI.</exception>
        public static T DownloadXml<T>(
            string baseUri,
            string fileName,
            string localFolder = null,
            bool alwaysReturns = true,
            int timeoutMilliseconds = 15000) where T : new()
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
                        // Read as text using UTF-8, then deserialize from string
                        var xml = File.ReadAllText(localPath, Encoding.UTF8);
                        return DeserializeFromString<T>(xml, alwaysReturns);
                    }
                }
                catch
                {
                    // Ignore and fall back to remote fetch
                }
            }
#endif

            var remoteUri = new Uri(baseUriObj, fileName);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(remoteUri);
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.Timeout = timeoutMilliseconds;

            try
            {
                using var response = request.GetResponse();
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream);
                var xml = reader.ReadToEnd();
                var result = DeserializeFromString<T>(xml, alwaysReturns);

#if DEBUG
                // Cache remote XML if a local folder is provided and file was not present
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

                        File.WriteAllText(localPath, xml, Encoding.UTF8);
                    }
                    catch
                    {
                        // Swallow caching errors in DEBUG mode
                    }
                }
#endif
                return result;
            }
            catch
            {
                return alwaysReturns ? new T() : default(T);
            }
        }

        /// <summary>
        /// Downloads and deserializes an XML file from the specified URI into an object of type <typeparamref
        /// name="T"/>.
        /// </summary>
        /// <remarks>If <paramref name="runAsync"/> is <see langword="false"/>, the operation is executed
        /// synchronously and the result is wrapped in a completed task. The method uses the specified <paramref
        /// name="timeoutMilliseconds"/> to limit the duration of the operation.</remarks>
        /// <typeparam name="T">The type of the object to deserialize the XML content into. Must have a parameterless constructor.</typeparam>
        /// <param name="baseUri">The base URI of the server from which the XML file will be downloaded.</param>
        /// <param name="fileName">The name of the XML file to download. This is appended to the <paramref name="baseUri"/> to form the full
        /// URI.</param>
        /// <param name="localFolder">An optional local folder path where the XML file may be cached or stored. If <c>null</c>, no local folder is
        /// used.</param>
        /// <param name="alwaysReturns">A value indicating whether the method should always return a result, even if the file cannot be downloaded.
        /// If <see langword="true"/>, a default instance of <typeparamref name="T"/> is returned in such cases.</param>
        /// <param name="runAsync">A value indicating whether the operation should be executed asynchronously. If <see langword="false"/>, the
        /// operation runs synchronously.</param>
        /// <param name="timeoutMilliseconds">The maximum time, in milliseconds, to wait for the operation to complete. Defaults to 15,000 milliseconds.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. If cancellation is requested, the operation is aborted.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the deserialized object of type
        /// <typeparamref name="T"/>.</returns>
        public static Task<T> DownloadXmlAsync<T>(
            string baseUri,
            string fileName,
            string localFolder = null,
            bool alwaysReturns = true,
            bool runAsync = true,
            int timeoutMilliseconds = 15000,
            CancellationToken cancellationToken = default) where T : new()
        {
            if (!runAsync)
            {
                return Task.FromResult(DownloadXml<T>(baseUri, fileName, localFolder, alwaysReturns, timeoutMilliseconds));
            }

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DownloadXml<T>(baseUri, fileName, localFolder, alwaysReturns, timeoutMilliseconds);
            }, cancellationToken);
        }

        #endregion Remote XML - Download + Deserialize (with local DEBUG fallback)

        #region Serialization - Core

        private static void SerializeInternal<T>(
            T instance,
            string path,
            bool writeThrough,
            bool useNamedMutex,
            CancellationToken cancellationToken)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tempPath = path + ".tmp";
            var gate = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            Mutex mutex = null;

            gate.Wait(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (useNamedMutex)
                {
                    mutex = CreatePathMutex(path);
                    mutex.WaitOne();
                }

                var options = writeThrough ? FileOptions.WriteThrough : FileOptions.None;
                using (var fs = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    options))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(fs, instance);
                    fs.Flush(true);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tempPath, path, destinationBackupFileName: null);
                    }
                    catch
                    {
                        File.Copy(tempPath, path, overwrite: true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            finally
            {
                try { if (File.Exists(tempPath)) { File.Delete(tempPath); } } catch { }
                try { mutex?.ReleaseMutex(); } catch { }
                mutex?.Dispose();
                gate.Release();
            }
        }

        #endregion Serialization - Core

        #region Deserialization - Core

        private static T DeserializeInternal<T>(
            string path,
            bool alwaysReturns,
            bool useNamedMutex,
            CancellationToken cancellationToken) where T : new()
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(path))
            {
                return alwaysReturns ? new T() : default(T);
            }

            var gate = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            Mutex mutex = null;

            gate.Wait(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (useNamedMutex)
                {
                    mutex = CreatePathMutex(path);
                    mutex.WaitOne();
                }

                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var serializer = new XmlSerializer(typeof(T));
                var obj = serializer.Deserialize(fs);
                return (T)obj;
            }
            catch
            {
                return alwaysReturns ? new T() : default(T);
            }
            finally
            {
                try { mutex?.ReleaseMutex(); } catch { }
                mutex?.Dispose();
                gate.Release();
            }
        }

        #endregion Deserialization - Core

        #region Infrastructure (Mutex + Hash)

        private static Mutex CreatePathMutex(string path)
        {
            var name = @"Global\Rappen.XTB.XmlAtomicSerializer-" + Sha1Hex(path.ToUpperInvariant());
            return new Mutex(false, name);
        }

        private static string Sha1Hex(string input)
        {
            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        #endregion Infrastructure (Mutex + Hash)
    }
}