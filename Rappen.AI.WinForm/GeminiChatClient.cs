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
        private readonly Dictionary<string, string> thoughtSignatures = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> functionNames = new(StringComparer.Ordinal);

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
                new StringContent(
                    new JavaScriptSerializer().Serialize(request),
                    Encoding.UTF8,
                    "application/json"),
                cancellationToken).ConfigureAwait(false);

            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Gemini request failed ({(int)response.StatusCode} {response.ReasonPhrase}): {responseText}");
            }

            var responseJson = new JavaScriptSerializer()
                .Deserialize<Dictionary<string, object>>(responseText);

            return new ChatResponse(GetResponseMessage(responseJson, responseText))
            {
                Usage = GetUsage(responseJson)
            };
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions options = null,
            CancellationToken cancellationToken = default)
        {
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
                var parts = CreateParts(message);

                if (parts.Count == 0)
                {
                    continue;
                }

                if (message.Role == ChatRole.System)
                {
                    systemParts.AddRange(parts);
                    continue;
                }

                contents.Add(new
                {
                    role = message.Role == ChatRole.Assistant ? "model" : "user",
                    parts
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

            var tools = CreateTools(options);
            if (tools.Count > 0)
            {
                request["tools"] = new[]
                {
                    new
                    {
                        functionDeclarations = tools
                    }
                };
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

        private List<object> CreateParts(ChatMessage message)
        {
            var parts = new List<object>();

            foreach (var functionCall in message.Contents.OfType<FunctionCallContent>())
            {
                var arguments = functionCall.Arguments?
                    .ToDictionary(argument => argument.Key, argument => (object)argument.Value)
                    ?? new Dictionary<string, object>();

                var geminiCallId = GetGeminiCallId(functionCall.CallId);

                var part = new Dictionary<string, object>
                {
                    ["functionCall"] = new
                    {
                        id = geminiCallId,
                        name = functionCall.Name,
                        args = arguments
                    }
                };

                if (TryGetThoughtSignature(functionCall.CallId, out var thoughtSignature))
                {
                    part["thoughtSignature"] = thoughtSignature;
                }

                parts.Add(part);
            }

            foreach (var functionResult in message.Contents.OfType<FunctionResultContent>())
            {
                parts.Add(new
                {
                    functionResponse = new
                    {
                        id = GetGeminiCallId(functionResult.CallId),
                        name = GetFunctionName(functionResult.CallId),
                        response = new
                        {
                            result = functionResult.Result?.ToString() ?? string.Empty
                        }
                    }
                });
            }

            if (parts.Count == 0 && !string.IsNullOrWhiteSpace(message.Text))
            {
                parts.Add(new { text = message.Text });
            }

            return parts;
        }

        private List<object> CreateTools(ChatOptions options)
        {
            var declarations = new List<object>();

            foreach (var function in options?.Tools?.OfType<AIFunction>() ?? Enumerable.Empty<AIFunction>())
            {
                var parameters = DeserializeSchema(function.JsonSchema.ToString());

                declarations.Add(new
                {
                    name = function.Name,
                    description = function.Description,
                    parameters
                });
            }

            return declarations;
        }

        private static object DeserializeSchema(string schema)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                return new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>()
                };
            }

            try
            {
                return new JavaScriptSerializer().DeserializeObject(schema);
            }
            catch
            {
                return new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>()
                };
            }
        }

        private ChatMessage GetResponseMessage(Dictionary<string, object> response, string responseText)
        {
            var candidate = GetArray(response, "candidates").FirstOrDefault();
            var content = GetDictionary(candidate, "content");
            var parts = GetArray(content, "parts").ToList();

            var functionCalls = new List<FunctionCallContent>();

            foreach (var part in parts)
            {
                var functionCall = GetDictionary(part, "functionCall");
                if (functionCall == null)
                {
                    continue;
                }

                var callId = GetString(functionCall, "id");
                var name = GetString(functionCall, "name");

                if (string.IsNullOrWhiteSpace(callId))
                {
                    callId = Guid.NewGuid().ToString("N");
                }

                var thoughtSignature = GetString(part, "thoughtSignature");
                if (!string.IsNullOrWhiteSpace(thoughtSignature))
                {
                    thoughtSignatures[callId] = thoughtSignature;
                }

                functionNames[callId] = name;

                var argumentsDictionary = functionCall.TryGetValue("args", out var argumentsValue) &&
                                          argumentsValue is Dictionary<string, object> arguments
                    ? arguments.ToDictionary(argument => argument.Key, argument => (object)argument.Value)
                    : new Dictionary<string, object>();

                functionCalls.Add(new FunctionCallContent(
                    callId,
                    name,
                    argumentsDictionary));
            }

            if (functionCalls.Count > 0)
            {
                return new ChatMessage(
                    ChatRole.Assistant,
                    functionCalls.Cast<AIContent>().ToArray());
            }

            var text = string.Concat(
                parts
                    .Select(part => GetString(part, "text"))
                    .Where(part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(text))
            {
                return new ChatMessage(ChatRole.Assistant, text);
            }

            var finishReason = GetString(candidate, "finishReason") ?? "none";

            throw new InvalidOperationException(
                $"Gemini returned neither text nor a function call. Finish reason: {finishReason}.{Environment.NewLine}{responseText}");
        }

        private static IEnumerable<Dictionary<string, object>> GetArray(Dictionary<string, object> value, string key)
        {
            if (value == null ||
                !value.TryGetValue(key, out var array) ||
                !(array is System.Collections.IEnumerable enumerable))
            {
                return Enumerable.Empty<Dictionary<string, object>>();
            }

            return enumerable
                .Cast<object>()
                .OfType<Dictionary<string, object>>();
        }

        private static Dictionary<string, object> GetDictionary(Dictionary<string, object> value, string key)
        {
            return value != null &&
                   value.TryGetValue(key, out var result)
                ? result as Dictionary<string, object>
                : null;
        }

        private static string GetString(Dictionary<string, object> value, string key)
        {
            return value != null &&
                   value.TryGetValue(key, out var result)
                ? result?.ToString()
                : null;
        }

        private string GetFunctionName(string callId) =>
            functionNames.TryGetValue(GetGeminiCallId(callId), out var name)
                ? name
                : "unknown";

        private static string GetGeminiCallId(string callId)
        {
            if (string.IsNullOrWhiteSpace(callId))
            {
                return callId;
            }

            var separator = callId.LastIndexOf(':');

            return separator >= 0
                ? callId.Substring(separator + 1)
                : callId;
        }

        private bool TryGetThoughtSignature(string callId, out string thoughtSignature)
        {
            return thoughtSignatures.TryGetValue(
                GetGeminiCallId(callId),
                out thoughtSignature);
        }
    }
}