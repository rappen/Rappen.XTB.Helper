using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Rappen.AI.WinForm
{
    public static class AiModelCatalog
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static bool CanDiscover(string provider, bool isFxbFreeProvider)
        {
            if (isFxbFreeProvider || string.IsNullOrWhiteSpace(provider))
            {
                return false;
            }

            return IsGemini(provider) ||
                   IsOpenAi(provider) ||
                   IsAnthropic(provider) ||
                   IsAzureOpenAi(provider);
        }

        public static async Task<IReadOnlyList<AiModel>> GetAsync(
            AiProvider provider,
            string endpoint,
            string apiKey,
            bool includePreviewExperimental,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("An API key is required to load available models.", nameof(apiKey));
            }

            IEnumerable<AiModel> models;

            if (IsGemini(provider.Name))
            {
                models = await GetGeminiModelsAsync(provider, apiKey, cancellationToken).ConfigureAwait(false);
            }
            else if (IsOpenAi(provider.Name))
            {
                models = await GetOpenAiModelsAsync(provider, apiKey, cancellationToken).ConfigureAwait(false);
            }
            else if (IsAnthropic(provider.Name))
            {
                models = await GetAnthropicModelsAsync(provider, apiKey, cancellationToken).ConfigureAwait(false);
            }
            else if (IsAzureOpenAi(provider.Name))
            {
                models = await GetAzureOpenAiDeploymentsAsync(provider, endpoint, apiKey, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException(
                    $"Loading available models is not implemented for provider '{provider.Name}'.");
            }

            return models
                .Where(model => includePreviewExperimental || !IsPreview(model.Name))
                .GroupBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderByDescending(model => GetModelVersion(model.Name))
                .ThenBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static async Task<IEnumerable<AiModel>> GetGeminiModelsAsync(
            AiProvider provider,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var models = new List<AiModel>();
            string pageToken = null;

            do
            {
                var url = provider.Endpoint +
                    "?key=" + Uri.EscapeDataString(apiKey) +
                    (string.IsNullOrWhiteSpace(pageToken)
                        ? string.Empty
                        : "&pageToken=" + Uri.EscapeDataString(pageToken));

                var response = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                var json = await ReadJsonAsync(response, "Gemini model discovery").ConfigureAwait(false);

                var pageModels = GetEnumerable(json, "models");
                foreach (var model in pageModels)
                {
                    var name = GetString(model, "name");
                    var supportedMethods = model.TryGetValue("supportedGenerationMethods", out var methods)
                        ? methods as IEnumerable
                        : null;

                    var supportsGenerateContent = supportedMethods?
                        .Cast<object>()
                        .Select(method => method?.ToString())
                        .Any(method => string.Equals(
                            method,
                            "generateContent",
                            StringComparison.OrdinalIgnoreCase)) == true;

                    if (string.IsNullOrWhiteSpace(name) ||
                        !name.StartsWith("models/gemini-", StringComparison.OrdinalIgnoreCase) ||
                        !supportsGenerateContent)
                    {
                        continue;
                    }

                    name = name.Substring("models/".Length);

                    if (!IsGeminiModelForNewUsers(name))
                    {
                        continue;
                    }

                    models.Add(CreateModel(provider, name, name));
                }

                pageToken = GetString(json, "nextPageToken");
            }
            while (!string.IsNullOrWhiteSpace(pageToken));

            return models;
        }

        private static async Task<IEnumerable<AiModel>> GetOpenAiModelsAsync(
            AiProvider provider,
            string apiKey,
            CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, provider.Endpoint))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var json = await ReadJsonAsync(response, "OpenAI model discovery").ConfigureAwait(false);

                return GetEnumerable(json, "data")
                    .Select(model => GetString(model, "id"))
                    .Where(IsRelevantOpenAiChatModel)
                    .Select(modelName => CreateModel(provider, modelName))
                    .ToList();
            }
        }

        private static async Task<IEnumerable<AiModel>> GetAnthropicModelsAsync(
            AiProvider provider,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var models = new List<AiModel>();
            string afterId = null;

            do
            {
                var url = provider.Endpoint +
                    (string.IsNullOrWhiteSpace(afterId)
                        ? string.Empty
                        : "?after_id=" + Uri.EscapeDataString(afterId));

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("x-api-key", apiKey);
                    request.Headers.Add("anthropic-version", "2023-06-01");

                    var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    var json = await ReadJsonAsync(response, "Anthropic model discovery").ConfigureAwait(false);

                    foreach (var model in GetEnumerable(json, "data"))
                    {
                        var name = GetString(model, "id");
                        if (string.IsNullOrWhiteSpace(name) ||
                            !name.StartsWith("claude-", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        models.Add(new AiModel
                        {
                            Name = name,
                            Url = GetDynamicModelUrl(provider, name)
                        });
                    }

                    afterId = GetString(json, "last_id");
                    if (!GetBoolean(json, "has_more"))
                    {
                        afterId = null;
                    }
                }
            }
            while (!string.IsNullOrWhiteSpace(afterId));

            return models;
        }

        private static async Task<IEnumerable<AiModel>> GetAzureOpenAiDeploymentsAsync(
            AiProvider provider,
            string endpoint,
            string apiKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException(
                    "An Azure OpenAI or Foundry endpoint is required to load deployments.",
                    nameof(endpoint));
            }

            var baseUrl = endpoint.TrimEnd('/');
            var url = baseUrl + "/openai/deployments?api-version=2024-10-21";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("api-key", apiKey);

                var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var json = await ReadJsonAsync(response, "Azure OpenAI deployment discovery").ConfigureAwait(false);

                return GetEnumerable(json, "data")
                    .Where(deployment =>
                        !string.Equals(GetString(deployment, "status"), "deleted", StringComparison.OrdinalIgnoreCase))
                    .Select(deployment =>
                    {
                        var model = GetDictionary(deployment, "model");
                        var name = GetString(deployment, "id");
                        var modelName = GetString(model, "name");

                        return new AiModel
                        {
                            Name = name,
                            Url = GetDynamicModelUrl(provider, name)
                        };
                    })
                    .Where(deployment => !string.IsNullOrWhiteSpace(deployment.Name))
                    .ToList();
            }
        }

        private static async Task<Dictionary<string, object>> ReadJsonAsync(
            HttpResponseMessage response,
            string operation)
        {
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"{operation} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {responseText}");
            }

            return new JavaScriptSerializer()
                .Deserialize<Dictionary<string, object>>(responseText);
        }

        private static bool IsGemini(string provider)
        {
            return string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOpenAi(string provider)
        {
            return string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAnthropic(string provider)
        {
            return string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAzureOpenAi(string provider)
        {
            return provider?.IndexOf("azure openai", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   provider?.IndexOf("foundry", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsRelevantOpenAiChatModel(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return false;
            }

            var name = modelName.ToLowerInvariant();

            if (IsDatedModelSnapshot(name))
            {
                return false;
            }

            if (name.Contains("embedding") ||
                name.Contains("moderation") ||
                name.Contains("image") ||
                name.Contains("audio") ||
                name.Contains("realtime") ||
                name.Contains("transcribe") ||
                name.Contains("tts") ||
                name.Contains("codex") ||
                name.Contains("safety") ||
                name.Contains("research"))
            {
                return false;
            }

            return name.StartsWith("gpt-", StringComparison.Ordinal) ||
                   name.StartsWith("chatgpt-", StringComparison.Ordinal) ||
                   name.StartsWith("o", StringComparison.Ordinal);
        }

        private static bool IsDatedModelSnapshot(string modelName)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                modelName,
                @"-\d{4}-\d{2}-\d{2}$");
        }

        private static bool IsPreview(string modelName)
        {
            var name = modelName?.ToLowerInvariant() ?? string.Empty;

            return name.Contains("preview") ||
                   name.Contains("experimental") ||
                   name.Contains("-exp") ||
                   name.Contains("nightly") ||
                   name.Contains("canary");
        }

        private static bool IsGeminiModelForNewUsers(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return false;
            }

            var name = modelName.ToLowerInvariant();

            // Google still lists some 2.x models although they cannot be used
            // by newly created Gemini API projects.
            if (name.StartsWith("gemini-2.", StringComparison.Ordinal))
            {
                return false;
            }

            return name.StartsWith("gemini-3.", StringComparison.Ordinal) &&
                   !name.Contains("embedding") &&
                   !name.Contains("image") &&
                   !name.Contains("audio") &&
                   !name.Contains("tts") &&
                   !name.Contains("live") &&
                   !name.Contains("robotics");
        }

        private static long GetModelVersion(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return 0;
            }

            var parts = modelName.Split('-', '_', '.');
            var version = new List<int>();

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var number))
                {
                    continue;
                }

                version.Add(number);
                if (version.Count == 3)
                {
                    break;
                }
            }

            while (version.Count < 3)
            {
                version.Add(0);
            }

            return (version[0] * 1000000L) +
                   (version[1] * 1000L) +
                   version[2];
        }

        private static Dictionary<string, object> GetDictionary(
            Dictionary<string, object> values,
            string key)
        {
            if (values == null || !values.TryGetValue(key, out var value))
            {
                return null;
            }

            return value as Dictionary<string, object>;
        }

        private static IEnumerable<Dictionary<string, object>> GetEnumerable(
            Dictionary<string, object> values,
            string key)
        {
            if (values == null ||
                !values.TryGetValue(key, out var value) ||
                !(value is IEnumerable enumerable))
            {
                return Enumerable.Empty<Dictionary<string, object>>();
            }

            return enumerable
                .Cast<object>()
                .OfType<Dictionary<string, object>>();
        }

        private static string GetString(Dictionary<string, object> values, string key)
        {
            return values != null &&
                   values.TryGetValue(key, out var value)
                ? value?.ToString()
                : null;
        }

        private static bool GetBoolean(Dictionary<string, object> values, string key)
        {
            return values != null &&
                   values.TryGetValue(key, out var value) &&
                   value != null &&
                   Convert.ToBoolean(value);
        }

        private sealed class NaturalModelNameComparer : IComparer<string>
        {
            public int Compare(string left, string right)
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                var leftParts = left.Split('-', '_', '.');
                var rightParts = right.Split('-', '_', '.');
                var length = Math.Max(leftParts.Length, rightParts.Length);

                for (var index = 0; index < length; index++)
                {
                    var leftPart = index < leftParts.Length ? leftParts[index] : string.Empty;
                    var rightPart = index < rightParts.Length ? rightParts[index] : string.Empty;

                    if (int.TryParse(leftPart, out var leftNumber) &&
                        int.TryParse(rightPart, out var rightNumber))
                    {
                        var numericComparison = rightNumber.CompareTo(leftNumber);
                        if (numericComparison != 0)
                        {
                            return numericComparison;
                        }

                        continue;
                    }

                    var textComparison = string.Compare(
                        leftPart,
                        rightPart,
                        StringComparison.OrdinalIgnoreCase);

                    if (textComparison != 0)
                    {
                        return textComparison;
                    }
                }

                return 0;
            }
        }

        private static AiModel CreateModel(
            AiProvider provider,
            string name,
            string documentationModelName = null)
        {
            var configuredModel = provider.Models?.FirstOrDefault(model =>
                string.Equals(model.Name, name, StringComparison.OrdinalIgnoreCase));

            return new AiModel
            {
                Name = name,
                Url = configuredModel?.Url
                    ?? GetDynamicModelUrl(provider, documentationModelName ?? name)
                    ?? provider.ModelsUrl
            };
        }

        private static string GetDynamicModelUrl(AiProvider provider, string name)
        {
            if (string.IsNullOrWhiteSpace(provider?.ModelsUrl) ||
                string.IsNullOrWhiteSpace(name))
            {
                return provider?.ModelsUrl;
            }

            var model = IsAnthropic(provider.Name) &&
                        name.StartsWith("claude-", StringComparison.OrdinalIgnoreCase)
                ? name.Substring("claude-".Length)
                : name;

            return provider.ModelsUrl.Replace("{model}", model);
        }
    }
}