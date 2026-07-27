using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace Rappen.AI.WinForm
{
    /// <summary>
    /// Handles GitHub Copilot authentication using the OAuth device-code flow and the
    /// short-lived Copilot token exchange, mirroring what the VS Code Copilot extension does.
    /// The long-lived GitHub OAuth token is stored (encrypted via DPAPI) by the caller;
    /// the short-lived Copilot API token is cached in memory here and refreshed on demand.
    /// </summary>
    public static class GitHubCopilotAuth
    {
        /// <summary>Provider name used in AiProvider/AiSettings and matched in AiCommunication.</summary>
        public const string ProviderName = "GitHubCopilot";

        // Public GitHub client id used by the VS Code Copilot editor integration.
        private const string ClientId = "01ab8ac9400c4e429b23";
        private const string Scope = "read:user";

        private const string DeviceCodeUrl = "https://github.com/login/device/code";
        private const string AccessTokenUrl = "https://github.com/login/oauth/access_token";
        private const string CopilotTokenUrl = "https://api.github.com/copilot_internal/v2/token";

        /// <summary>OpenAI-compatible Copilot chat endpoint.</summary>
        public const string ApiBaseUrl = "https://api.githubcopilot.com";

        // Copilot editor-style request headers (required by the Copilot API).
        public const string EditorVersion = "vscode/1.96.0";
        public const string EditorPluginVersion = "copilot-chat/0.23.0";
        public const string CopilotIntegrationId = "vscode-chat";
        public const string UserAgent = "GitHubCopilotChat/0.23.0";

        private static readonly HttpClient http = new HttpClient();

        // In-memory cache of the short-lived Copilot API token, keyed by the GitHub token.
        private static string cachedForGitHubToken;
        private static string cachedCopilotToken;
        private static DateTime cachedExpiryUtc = DateTime.MinValue;
        private static readonly object cacheLock = new object();

        #region Device flow

        public sealed class DeviceCodeInfo
        {
            public string DeviceCode { get; set; }
            public string UserCode { get; set; }
            public string VerificationUri { get; set; }
            public int ExpiresInSeconds { get; set; }
            public int IntervalSeconds { get; set; }
        }

        /// <summary>
        /// Step 1: request a device + user code from GitHub. Show the UserCode and
        /// VerificationUri to the user, then call <see cref="WaitForAccessToken"/>.
        /// </summary>
        public static DeviceCodeInfo RequestDeviceCode()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("scope", Scope)
            });
            using (var request = new HttpRequestMessage(HttpMethod.Post, DeviceCodeUrl) { Content = content })
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                var response = http.SendAsync(request).GetAwaiter().GetResult();
                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    return new DeviceCodeInfo
                    {
                        DeviceCode = GetString(root, "device_code"),
                        UserCode = GetString(root, "user_code"),
                        VerificationUri = GetString(root, "verification_uri"),
                        ExpiresInSeconds = GetInt(root, "expires_in", 900),
                        IntervalSeconds = GetInt(root, "interval", 5)
                    };
                }
            }
        }

        /// <summary>
        /// Step 2: poll GitHub until the user authorizes (or the flow fails/expires).
        /// Returns the long-lived GitHub OAuth access token. Blocking; run on a worker thread.
        /// </summary>
        /// <param name="device">The info from <see cref="RequestDeviceCode"/>.</param>
        /// <param name="isCancelled">Optional callback to abort polling.</param>
        public static string WaitForAccessToken(DeviceCodeInfo device, Func<bool> isCancelled = null)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }
            var interval = Math.Max(1, device.IntervalSeconds);
            var deadline = DateTime.UtcNow.AddSeconds(device.ExpiresInSeconds > 0 ? device.ExpiresInSeconds : 900);
            while (DateTime.UtcNow < deadline)
            {
                if (isCancelled != null && isCancelled())
                {
                    return null;
                }
                Thread.Sleep(interval * 1000);
                if (isCancelled != null && isCancelled())
                {
                    return null;
                }

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", ClientId),
                    new KeyValuePair<string, string>("device_code", device.DeviceCode),
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
                });
                using (var request = new HttpRequestMessage(HttpMethod.Post, AccessTokenUrl) { Content = content })
                {
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                    var response = http.SendAsync(request).GetAwaiter().GetResult();
                    var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        var accessToken = GetString(root, "access_token");
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            return accessToken;
                        }
                        var error = GetString(root, "error");
                        switch (error)
                        {
                            case "authorization_pending":
                                break;
                            case "slow_down":
                                interval += 5;
                                break;
                            case "expired_token":
                            case "access_denied":
                                throw new InvalidOperationException($"GitHub sign-in failed: {error}");
                            default:
                                if (!string.IsNullOrWhiteSpace(error))
                                {
                                    throw new InvalidOperationException($"GitHub sign-in failed: {error}");
                                }
                                break;
                        }
                    }
                }
            }
            throw new TimeoutException("GitHub sign-in timed out. Please try again.");
        }

        #endregion Device flow

        #region Copilot token exchange

        /// <summary>
        /// Returns a valid short-lived Copilot API token, exchanging the GitHub OAuth token when
        /// the cached token is missing or (nearly) expired. Thread-safe. Blocking.
        /// </summary>
        public static string GetCopilotToken(string gitHubToken, bool forceRefresh = false)
        {
            if (string.IsNullOrWhiteSpace(gitHubToken))
            {
                throw new InvalidOperationException("Not signed in to GitHub Copilot.");
            }
            lock (cacheLock)
            {
                if (!forceRefresh &&
                    gitHubToken == cachedForGitHubToken &&
                    !string.IsNullOrEmpty(cachedCopilotToken) &&
                    DateTime.UtcNow < cachedExpiryUtc.AddMinutes(-2))
                {
                    return cachedCopilotToken;
                }

                using (var request = new HttpRequestMessage(HttpMethod.Get, CopilotTokenUrl))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "token " + gitHubToken);
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                    request.Headers.TryAddWithoutValidation("Editor-Version", EditorVersion);
                    request.Headers.TryAddWithoutValidation("Editor-Plugin-Version", EditorPluginVersion);
                    var response = http.SendAsync(request).GetAwaiter().GetResult();
                    var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"Could not get a Copilot token (HTTP {(int)response.StatusCode}). " +
                            "Make sure your GitHub account has an active Copilot subscription.");
                    }
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        var token = GetString(root, "token");
                        if (string.IsNullOrWhiteSpace(token))
                        {
                            throw new InvalidOperationException("Copilot token response did not contain a token.");
                        }
                        var expiresAt = GetLong(root, "expires_at", 0);
                        cachedForGitHubToken = gitHubToken;
                        cachedCopilotToken = token;
                        cachedExpiryUtc = expiresAt > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(expiresAt).UtcDateTime
                            : DateTime.UtcNow.AddMinutes(25);
                        return token;
                    }
                }
            }
        }

        /// <summary>Clears the in-memory Copilot token cache (e.g. on sign out).</summary>
        public static void ClearCache()
        {
            lock (cacheLock)
            {
                cachedForGitHubToken = null;
                cachedCopilotToken = null;
                cachedExpiryUtc = DateTime.MinValue;
            }
        }

        #endregion Copilot token exchange

        #region Models

        /// <summary>
        /// Fetches the chat models the signed-in account can use from the Copilot /models
        /// endpoint. Returns an empty list on any failure so the caller can fall back to defaults.
        /// Blocking.
        /// </summary>
        public static List<AiModel> GetModels(string gitHubToken)
        {
            var models = new List<AiModel>();
            if (string.IsNullOrWhiteSpace(gitHubToken))
            {
                return models;
            }
            try
            {
                var copilotToken = GetCopilotToken(gitHubToken);
                using (var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + "/models"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + copilotToken);
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                    request.Headers.TryAddWithoutValidation("Editor-Version", EditorVersion);
                    request.Headers.TryAddWithoutValidation("Editor-Plugin-Version", EditorPluginVersion);
                    request.Headers.TryAddWithoutValidation("Copilot-Integration-Id", CopilotIntegrationId);
                    var response = http.SendAsync(request).GetAwaiter().GetResult();
                    var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        return models;
                    }
                    return ParseModels(json);
                }
            }
            catch
            {
                // Network/parse failure -> caller falls back to the default list.
            }
            return models;
        }

        /// <summary>
        /// Parses a Copilot /models JSON payload into the list of usable chat models. Pure and
        /// network-free (unit-testable): keeps chat-capable, model-picker-enabled entries,
        /// de-duplicated by id (case-insensitive). Returns an empty list for null/blank/invalid input.
        /// </summary>
        /// <param name="json">The raw JSON body returned by the /models endpoint.</param>
        public static List<AiModel> ParseModels(string json)
        {
            var models = new List<AiModel>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return models;
            }
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                    {
                        return models;
                    }
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in data.EnumerateArray())
                    {
                        var id = GetString(item, "id");
                        if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                        {
                            continue;
                        }
                        // Keep only chat-capable models.
                        if (item.TryGetProperty("capabilities", out var caps) &&
                            caps.TryGetProperty("type", out var type) &&
                            type.ValueKind == JsonValueKind.String &&
                            !string.Equals(type.GetString(), "chat", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        // Skip models not offered in the model picker (e.g. embeddings, disabled).
                        if (item.TryGetProperty("model_picker_enabled", out var picker) &&
                            picker.ValueKind == JsonValueKind.False)
                        {
                            continue;
                        }
                        models.Add(new AiModel { Name = id, Endpoint = ApiBaseUrl });
                    }
                }
            }
            catch
            {
                // Invalid JSON -> empty list; caller falls back to the default list.
                return new List<AiModel>();
            }
            return models;
        }

        #endregion Models

        #region DPAPI storage helpers

        // Secret storage delegates to the reusable SecretProtector (Windows DPAPI, CurrentUser).
        // The entropy below is specific to this provider's token, so adding more OAuth providers
        // later means each supplies its own entropy and none can decrypt another's secret.
        // This is a DELIBERATE choice over XrmToolBox's reversible CryptoManager scheme; see
        // SecretProtector for the full rationale (machine/user-bound, no embedded key, safe on roam).
        private static readonly byte[] Entropy = SecretProtector.EntropyFromLabel("Rappen.AI.WinForm.GitHubCopilot.v1");

        /// <summary>Encrypts a secret for the current Windows user (DPAPI). Returns base64.</summary>
        public static string Protect(string plainText) => SecretProtector.Protect(plainText, Entropy);

        /// <summary>Decrypts a base64 secret produced by <see cref="Protect"/>. Returns empty on failure.</summary>
        public static string Unprotect(string protectedBase64) => SecretProtector.Unprotect(protectedBase64, Entropy);

        #endregion DPAPI storage helpers

        #region Provider factory

        /// <summary>
        /// Builds the local AiProvider definition for GitHub Copilot. Injected into the provider
        /// list at runtime until it is added to the online configuration.
        /// </summary>
        public static AiProvider CreateProvider()
        {
            return new AiProvider
            {
                Name = ProviderName,
                FullName = "GitHub Copilot",
                Url = "https://docs.github.com/copilot",
                EndpointFixed = true,
                OAuth = true,
                Free = false,
                Models = new List<AiModel>
                {
                    new AiModel { Name = "gpt-4o", Endpoint = ApiBaseUrl },
                    new AiModel { Name = "gpt-4.1", Endpoint = ApiBaseUrl },
                    new AiModel { Name = "gpt-5", Endpoint = ApiBaseUrl },
                    new AiModel { Name = "gpt-5-mini", Endpoint = ApiBaseUrl }
                }
            };
        }

        #endregion Provider factory

        #region JSON helpers

        private static string GetString(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static int GetInt(JsonElement root, string name, int fallback)
        {
            return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : fallback;
        }

        private static long GetLong(JsonElement root, string name, long fallback)
        {
            return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
                ? value.GetInt64()
                : fallback;
        }

        #endregion JSON helpers
    }
}
