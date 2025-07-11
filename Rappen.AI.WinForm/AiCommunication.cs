using Anthropic;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
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
        /// <param name="prompt">The question/statement from you to the AI</param>
        /// <param name="supplier">AI supplier, e.g. 'OpenAI' or 'Anthropic'</param>
        /// <param name="model">The model/version of the supplier</param>
        /// <param name="apikey">The API key needed to know you account</param>
        /// <param name="chatMessageHistory">We are containing the chat history, it helps the AI, and this method may add more to it</param>
        /// <param name="tool">The tool in XrmToolBox that is calling this method</param>
        /// <param name="handleResponse">The method that handles the response from AI</param>
        /// <param name="internalTools">This may containg 0-x methods that can be called inside this method, bepending on what the AI may need/help us</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void CallingAI(string prompt, string supplier, string model, string apikey, ChatMessageHistory chatMessageHistory, PluginControlBase tool, Action<ChatResponse> handleResponse, params Func<string, string>[] internalTools)
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

            chatMessageHistory.Add(ChatRole.User, prompt, false);
            chatMessageHistory.IsRunning = true;

            tool.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Asking {supplier}...",
                Work = (w, a) =>
                {
                    a.Result = AskAI(supplier, model, apikey, chatMessageHistory, internalTools);
                },
                PostWorkCallBack = (w) =>
                {
                    tool.Cursor = Cursors.Default;
                    chatMessageHistory.IsRunning = false;
                    if (w.Error != null)
                    {
                        tool.LogError($"Error while communicating with {supplier}\n{w.Error.ExceptionDetails()}\n{w.Error}\n{w.Error.StackTrace}");
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
                            tool.ShowErrorDialog(w.Error, "AI Communitation", $"{supplier} {model}");
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
        /// Perform a 'Sampling' request to the AI model. 'Sampling' is a concept from Model Context Protocol (MCP) where an AI-function can call the AI internally, without any support for function calling.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string SamplingAI(string systemPrompt, string userPrompt, string supplier, string model, string apikey)
        {
            using (IChatClient chatClient = GetChatClient(supplier, model, apikey).Build())
            {
                var chatMessages = new List<Microsoft.Extensions.AI.ChatMessage>
                {
                    new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, systemPrompt),
                    new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userPrompt)
                };

                var response = chatClient
                    .GetResponseAsync(chatMessages)
                    .GetAwaiter()
                    .GetResult();
                return response.Text;
            }
        }

        private static ChatResponse AskAI(string supplier, string model, string apikey, ChatMessageHistory chatMessageHistory, params Func<string, string>[] internalTools)
        {
            using (IChatClient chatClient = GetChatClient(supplier, model, apikey).UseFunctionInvocation().Build())
            {
                var chatOptions = new ChatOptions();
                if (internalTools?.Count() > 0)
                {
                    chatOptions.Tools = internalTools.Select(tool => AIFunctionFactory.Create(tool) as AITool).ToList();
                }

                var response = chatClient
                    .GetResponseAsync(chatMessageHistory.Messages, chatOptions)
                    .GetAwaiter()
                    .GetResult();
                return response;
            }
        }

        private static ChatClientBuilder GetChatClient(string supplier, string model, string apikey)
        {
            IChatClient client = supplier == "Anthropic" ? new AnthropicClient(apikey) :
            supplier == "OpenAI" ? new ChatClient(model, apikey).AsIChatClient() :
            throw new NotImplementedException($"AI Supplier {supplier} not implemented!");

            return client.AsBuilder().ConfigureOptions(options =>
            {
                options.ModelId = model;
                options.MaxOutputTokens = 4096;
            });
        }

        private static string ExceptionDetails(this Exception ex, int level = 0) => ex == null ? string.Empty :
            $"{new string(' ', level * 2)}{ex.Message}{Environment.NewLine}{ex.InnerException.ExceptionDetails(level + 1)}".Trim();
    }
}