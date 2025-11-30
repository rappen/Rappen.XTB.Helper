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
        /// Calls the AI model with the given prompt and handles the response.
        /// </summary>
        /// <param name="tool">The tool in XrmToolBox that is calling this method</param>
        /// <param name="chatMessageHistory">We are containing the chat history, it helps the AI, and this method may add more to it</param>
        /// <param name="apikey">The API key needed to know you account</param>
        /// <param name="prompt">The question/statement from you to the AI</param>
        /// <param name="handleResponse">The method that handles the response from AI</param>
        /// <param name="internalTools">This may containg 0-x methods that can be called inside this method, bepending on what the AI may need/help us</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void CallingAIAsync(PluginControlBase tool, ChatMessageHistory chatMessageHistory, string prompt, Action<ChatResponse> handleResponse, params Func<string, string>[] internalTools)
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
                Message = $"Asking {chatMessageHistory.Provider}...",
                Work = (w, a) =>
                {
                    a.Result = CallingAI(clientBuilder, chatMessageHistory, internalTools);
                },
                PostWorkCallBack = (w) =>
                {
                    tool.Cursor = Cursors.Default;
                    chatMessageHistory.IsRunning = false;
                    if (w.Error != null)
                    {
                        tool.LogError($"Error while communicating with {chatMessageHistory.Provider}\n{w.Error.ExceptionDetails()}\n{w.Error}\n{w.Error.StackTrace}");
                        var apiEx = w.Error as ApiException ?? w.Error.InnerException as ApiException;
                        if (apiEx != null && apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            tool.ShowErrorDialog(new Exception("ApiKey may be incorrect."), "AI Communitation", w.Error.ExceptionDetails());
                        }
                        else if (apiEx != null && (int)apiEx.StatusCode == 529) // Anthropic service overloaded
                        {
                            tool.ShowErrorDialog(new Exception("AI service is overloaded, please try again later."), "AI Communitation", w.Error.ExceptionDetails());
                        }
                        else if (w.Error is MissingMethodException missmeth)
                        {
                            tool.ShowErrorDialog(new Exception($"There is a conflict between tools, where the other tool loads a too old version that {tool.ToolName} needs. Please click 'Create Issue' below to give developers details so it can be solved!"), "AI Communitation", w.Error.ExceptionDetails());
                        }
                        else
                        {
                            tool.ShowErrorDialog(w.Error, "AI Communitation", $"{chatMessageHistory.Provider} {chatMessageHistory.Model}");
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
        /// Calls the AI model with the given prompt and returns the response.
        /// </summary>
        /// <param name="chatMessageHistory"></param>
        /// <param name="apikey"></param>
        /// <param name="prompt"></param>
        /// <param name="internalTools"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ChatResponse CallingAI(ChatMessageHistory chatMessageHistory, string prompt, params Func<string, string>[] internalTools)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return null;
            }
            if (!chatMessageHistory.Initialized)
            {
                throw new InvalidOperationException("ChatMessageHistory is not initialized. Please call InitializeIfNeeded with a system prompt before using this method.");
            }

            chatMessageHistory.Add(ChatRole.User, prompt);
            chatMessageHistory.IsRunning = true;

            var clientBuilder = GetChatClientBuilder(chatMessageHistory);

            var result = CallingAI(clientBuilder, chatMessageHistory, internalTools);
            chatMessageHistory.IsRunning = false;
            if (result == null)
            {
                throw new InvalidOperationException("AI response is null. Please check the AI communication.");
            }
            chatMessageHistory.Add(result);
            return result;
        }

        /// <summary>
        /// Perform a 'Sampling' request to the AI model. 'Sampling' is a concept from Model Context Protocol (MCP) where an AI-function can call the AI internally, without any support for function calling.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ChatResponse SamplingAI(ChatMessageHistory chatMessageHistory, string systemPrompt, string userPrompt, string internalMessage)
        {
            if (!string.IsNullOrWhiteSpace(internalMessage))
            {
                chatMessageHistory.Add(ChatRole.Assistant, internalMessage, false, true);
            }
            using (IChatClient chatClient = GetChatClientBuilder(chatMessageHistory).Build())
            {
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
        }

        private static ChatResponse CallingAI(ChatClientBuilder clientBuilder, ChatMessageHistory chatMessageHistory, params Func<string, string>[] internalTools)
        {
            using (IChatClient chatClient = clientBuilder.UseFunctionInvocation().Build())
            {
                var chatOptions = new ChatOptions();
                if (internalTools?.Count() > 0)
                {
                    chatOptions.Tools = internalTools.Select(tool => AIFunctionFactory.Create(tool) as AITool).ToList();
                }

                optionallyAddReasoningEffortLevel(chatMessageHistory, chatOptions);

                var response = chatClient
                    .GetResponseAsync(chatMessageHistory.Messages, chatOptions)
                    .GetAwaiter()
                    .GetResult();
                return response;
            }
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

            if (chatMessageHistory.Provider == "OpenAI" && chatMessageHistory.Model.ToLowerInvariant().Equals("gpt-5.1")) {
                return; // gpt-5.1 already defaults to reasoning level = "None", meaning no reasoning.
            }
            else if (chatMessageHistory.Provider == "OpenAI" && allowedModels.Contains(chatMessageHistory.Model))
            {
                // Other gpt-5 models (gpt-5, gpt-5-mini, gpt-5-nano) defaults to reasoning level "medium".
                var chatCompletionOptions = new ChatCompletionOptions();

#pragma warning disable OPENAI001
                chatCompletionOptions.ReasoningEffortLevel = ChatReasoningEffortLevel.Low;
                chatOptions.RawRepresentationFactory = _ => chatCompletionOptions;
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
}