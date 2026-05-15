using Anthropic;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Rappen.XRM.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Rappen.AI.WinForm
{
    public static class AiCommunication
    {
        /// <summary>
        /// Prompts the AI model with the given prompt and handles the response.
        /// </summary>
        /// <param name="tool">The tool in XrmToolBox that is calling this method</param>
        /// <param name="chatMessageHistory">We are containing the chat history, it helps the AI, and this method may add more to it</param>
        /// <param name="prompt">The question/statement from you to the AI</param>
        /// <param name="handleResponse">The method that handles the response from AI</param>
        /// <param name="internalTools">This may containg 0-x methods that can be called inside this method, bepending on what the AI may need/help us</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Prompt(PluginControlBase tool, ChatMessageHistory chatMessageHistory, string prompt, Action<ChatResponse> handleResponse, params AiInternalTool[] internalTools)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return;
            }
            if (!chatMessageHistory.Initialized)
            {
                throw new InvalidOperationException("ChatMessageHistory is not initialized. Please call InitializeIfNeeded with a system prompt before using this method.");
            }
            tool.Cursor = Cursors.WaitCursor;

            chatMessageHistory.Add(ChatRole.User, prompt);
            chatMessageHistory.IsRunning = true;

            var clientBuilder = GetChatClientBuilder(chatMessageHistory);

            tool.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Asking {chatMessageHistory.ProviderDisplayName}...",
                Work = (w, a) =>
                {
                    a.Result = PromptTools(clientBuilder, chatMessageHistory, internalTools);
                },
                PostWorkCallBack = (w) =>
                {
                    tool.Cursor = Cursors.Default;
                    chatMessageHistory.IsRunning = false;
                    if (w.Error != null)
                    {
                        tool.LogError($"Error while communicating with {chatMessageHistory.ProviderDisplayName}\n{w.Error.ExceptionDetails()}\n{w.Error}\n{w.Error.StackTrace}");
                        if (w.Error is MissingMethodException)
                        {
                            tool.ShowErrorDialog(new Exception($"There is a conflict between tools, where the other tool loads a too old version that {tool.ToolName} needs. Please click 'Create Issue' below to give developers details so it can be solved!"), "AI Communication", w.Error.ExceptionDetails());
                        }
                        else
                        {
                            var errorKind = AiErrorClassifier.Classify(w.Error);
                            if (errorKind == AiErrorKind.Unknown)
                            {
                                tool.ShowErrorDialog(w.Error, "AI Communication", $"{chatMessageHistory.ProviderDisplayName} {chatMessageHistory.Model}");
                            }
                            else
                            {
                                tool.ShowErrorDialog(new Exception(AiErrorClassifier.UserMessage(errorKind)), "AI Communication", w.Error.ExceptionDetails());
                            }
                        }
                        handleResponse?.Invoke(null);
                    }
                    else if (w.Result is ChatResponse response)
                    {
                        chatMessageHistory.Add(response);
                        handleResponse?.Invoke(response);
                    }
                }
            });
        }

        /// <summary>
        /// Prompts the AI model with a stateless request using its own system prompt and user prompt, without tool invocation.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ChatResponse PromptStateless(ChatMessageHistory chatMessageHistory, string systemPrompt, string userPrompt, string internalMessage)
        {
            if (!string.IsNullOrWhiteSpace(internalMessage))
            {
                chatMessageHistory.Add(ChatRole.Assistant, internalMessage, false, true);
            }
            using var chatClient = GetChatClientBuilder(chatMessageHistory).Build();
            var chatMessages = new List<Microsoft.Extensions.AI.ChatMessage>
                {
                    new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, systemPrompt),
                    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userPrompt)
                };

            var chatOptions = new ChatOptions();
            optionallyAddReasoningEffortLevel(chatMessageHistory, chatOptions);

            var response = chatClient
                .GetResponseAsync(chatMessages, chatOptions)
                .GetAwaiter()
                .GetResult();

            return response;
        }

        private static ChatResponse PromptTools(ChatClientBuilder clientBuilder, ChatMessageHistory chatMessageHistory, params AiInternalTool[] internalTools)
        {
            using var chatClient = clientBuilder.UseFunctionInvocation().Build();
            var chatOptions = new ChatOptions();
            if (internalTools != null && internalTools.Length > 0)
            {
                chatOptions.Tools = internalTools
                    .Select(tool => AIFunctionFactory.Create(
                        tool.Callback,
                        name: tool.Name,
                        description: tool.Description) as AITool)
                    .Where(tool => tool != null)
                    .ToList();
            }

            optionallyAddReasoningEffortLevel(chatMessageHistory, chatOptions);

            var response = chatClient
                .GetResponseAsync(chatMessageHistory.Messages, chatOptions)
                .GetAwaiter()
                .GetResult();
            return response;
        }

        /// <summary>
        /// Set reasoning level to lowest value for OpenAI models, to increase inference speed.
        /// </summary>
        /// <param name="chatMessageHistory"></param>
        /// <param name="chatOptions"></param>
        private static void optionallyAddReasoningEffortLevel(ChatMessageHistory chatMessageHistory, ChatOptions chatOptions)
        {
            var allowedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "gpt-5",
                "gpt-5-mini",
                "gpt-5-nano"
            };

            if (chatMessageHistory.Provider == "OpenAI" && chatMessageHistory.Model.ToLowerInvariant().Equals("gpt-5.1"))
            {
                return; // gpt-5.1 already defaults to reasoning level = "None", meaning no reasoning.
            }
            else if (chatMessageHistory.Provider == "OpenAI" && allowedModels.Contains(chatMessageHistory.Model))
            {
                // Other gpt-5 models (gpt-5, gpt-5-mini, gpt-5-nano) defaults to reasoning level "medium".
                var chatCompletionOptions = new ChatCompletionOptions();

#pragma warning disable OPENAI001
                chatCompletionOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Low;
                chatOptions.RawRepresentationFactory = _ => chatCompletionOptions;
#pragma warning restore OPENAI001
            }
        }

        private static ChatClientBuilder GetChatClientBuilder(ChatMessageHistory chatMessageHistory)
        {
            IChatClient client = null;
            if (chatMessageHistory.Provider == "Anthropic")
            {
                client = new AnthropicClient(chatMessageHistory.ApiKey);
            }
            else if (chatMessageHistory.Provider == "OpenAI")
            {
                client = new ChatClient(chatMessageHistory.Model, chatMessageHistory.ApiKey).AsIChatClient();
            }
            else if (chatMessageHistory.Provider.ToLowerInvariant().Contains("foundry") &&
                     chatMessageHistory.Model.ToLowerInvariant().Contains("gpt"))
            {
                client = new AzureOpenAIClient(
                    new Uri(chatMessageHistory.Endpoint),
                    new AzureKeyCredential(chatMessageHistory.ApiKey))
                .GetChatClient(chatMessageHistory.Model).AsIChatClient();
            }
            if (client == null)
            {
                throw new NotImplementedException($"AI provider '{chatMessageHistory.Provider}' not (yet?) implemented.");
            }

            return client.AsBuilder().ConfigureOptions(options =>
            {
                options.ModelId = chatMessageHistory.Model;
                //options.MaxOutputTokens = 4096;       // accepterar inte Azure.AI !
            });
        }
    }

    public sealed class AiInternalTool
    {
        public AiInternalTool(Func<string, string> callback, string name, string description)
        {
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            Name = name;
            Description = description;
        }

        public Func<string, string> Callback { get; }
        public string Name { get; }
        public string Description { get; }
    }
}
