using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Rappen.AI.WinForm
{
    internal sealed class GeminiChatClient : IChatClient
    {
        private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string apiKey;
        private readonly string model;

        public GeminiChatClient(string model, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("A Gemini model is required.", nameof(model));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("A Gemini API key is required.", nameof(apiKey));
            }

            this.model = model;
            this.apiKey = apiKey;
        }

        public ChatClientMetadata Metadata => new ChatClientMetadata("Gemini");

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(messages, options);
            var response = await httpClient.PostAsync(
                GetEndpoint(),
                CreateJsonContent(request),
                cancellationToken).ConfigureAwait(false);

            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Gemini request failed ({(int)response.StatusCode} {response.ReasonPhrase}): {responseText}");
            }

            var responseJson = new JavaScriptSerializer()
                .Deserialize<Dictionary<string, object>>(responseText);

            return new ChatResponse(
                new ChatMessage(ChatRole.Assistant, GetResponseText(responseJson)))
            {
                Usage = GetUsage(responseJson)
            };
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield break;
            }

            throw new NotSupportedException("Gemini streaming has not yet been implemented.");
        }

        public object GetService(Type serviceType, object serviceKey = null)
        {
            return serviceType == typeof(IChatClient) ||
                   serviceType == typeof(GeminiChatClient)
                ? this
                : null;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        private object CreateRequest(IEnumerable<ChatMessage> messages, ChatOptions options)
        {
            var systemParts = new List<object>();
            var contents = new List<object>();

            foreach (var message in messages ?? Enumerable.Empty<ChatMessage>())
            {
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    continue;
                }

                if (message.Role == ChatRole.System)
                {
                    systemParts.Add(new { text = message.Text });
                    continue;
                }

                contents.Add(new
                {
                    role = message.Role == ChatRole.Assistant ? "model" : "user",
                    parts = new[] { new { text = message.Text } }
                });
            }

            var request = new Dictionary<string, object>
            {
                ["contents"] = contents
            };

            if (systemParts.Count > 0)
            {
                request["systemInstruction"] = new { parts = systemParts };
            }

            if (options?.Temperature != null || options?.MaxOutputTokens != null)
            {
                request["generationConfig"] = new
                {
                    temperature = options?.Temperature,
                    maxOutputTokens = options?.MaxOutputTokens
                };
            }

            return request;
        }

        private string GetEndpoint()
        {
            return ApiBaseUrl +
                Uri.EscapeDataString(model) +
                ":generateContent?key=" +
                Uri.EscapeDataString(apiKey);
        }

        private static StringContent CreateJsonContent(object request)
        {
            var json = new JavaScriptSerializer().Serialize(request);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static string GetResponseText(Dictionary<string, object> response)
        {
            var candidates = response["candidates"] as System.Collections.IEnumerable;
            var candidate = candidates?
                .Cast<object>()
                .OfType<Dictionary<string, object>>()
                .FirstOrDefault();

            var content = candidate?["content"] as Dictionary<string, object>;
            var parts = content?["parts"] as System.Collections.IEnumerable;

            var text = string.Concat(
                parts?
                    .Cast<object>()
                    .OfType<Dictionary<string, object>>()
                    .Select(part => part.ContainsKey("text") ? part["text"]?.ToString() : null)
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                ?? Enumerable.Empty<string>());

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException(
                    $"Gemini returned no text response: {response}");
            }

            return text;
        }

        private static UsageDetails GetUsage(Dictionary<string, object> response)
        {
            var usage = response.TryGetValue("usageMetadata", out var value)
                ? value as Dictionary<string, object>
                : null;

            if (usage == null)
            {
                return null;
            }

            return new UsageDetails
            {
                InputTokenCount = GetLong(usage, "promptTokenCount"),
                OutputTokenCount = GetLong(usage, "candidatesTokenCount"),
                TotalTokenCount = GetLong(usage, "totalTokenCount")
            };
        }

        private static long? GetLong(Dictionary<string, object> values, string key)
        {
            if (!values.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            return Convert.ToInt64(value);
        }
    }
}