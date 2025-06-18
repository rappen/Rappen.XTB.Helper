using Anthropic;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Rappen.AI.WinForm
{
    public static class AiCommunication
    {
        public static void CallingAI(string prompt, AiSupplier supplier, AiModel model, string apikey, ChatMessageHistory chatMessageHistory, PluginControlBase tool, Func<string, string> executeRequest, Action<ChatResponse> handleResponse)
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

            tool.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Asking the {supplier.Name}...",
                Work = (w, a) =>
                {
                    a.Result = AskAI(supplier, model, apikey, chatMessageHistory, executeRequest);
                },
                PostWorkCallBack = (w) =>
                {
                    tool.Cursor = Cursors.Default;
                    if (w.Error != null)
                    {
                        tool.LogError($"Error while communicating with {supplier.Name}\n{w.Error.ExceptionDetails()}\n{w.Error}\n{w.Error.StackTrace}");
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
                            tool.ShowErrorDialog(new Exception("There might be a conflict between tools, where the other tool loads a too old version, probably about Microsoft.Bcl.AsyncInterfaces."), "AI Communitation", w.Error.ExceptionDetails());
                        }
                        else
                        {
                            tool.ShowErrorDialog(w.Error, "AI Communitation", $"{supplier} {model}");
                        }
                    }
                    else if (w.Result is ChatResponse response)
                    {
                        chatMessageHistory.Add(response);
                        handleResponse?.Invoke(response);
                    }
                }
            });
        }

        private static ChatResponse AskAI(AiSupplier supplier, AiModel model, string apikey, ChatMessageHistory chatMessageHistory, Func<string, string> executeRequest)
        {
            using (IChatClient client =
                supplier.Name == "Anthropic" ? new AnthropicClient(apikey) :
                supplier.Name == "OpenAI" ? new ChatClient(model.Name, apikey).AsIChatClient() :
                throw new NotImplementedException(String.Format("AI Supplier {0} not implemented!", supplier.Name)))
            {
                var chatClient = client.AsBuilder().ConfigureOptions(options =>
                {
                    options.ModelId = model.Name;
                    options.MaxOutputTokens = 4096;
                }).UseFunctionInvocation().Build();

                var chatOptions = new ChatOptions();
                if (executeRequest != null)
                {
                    chatOptions.Tools = new List<AITool> { AIFunctionFactory.Create(executeRequest) };
                }

                var response = chatClient
                    .GetResponseAsync(chatMessageHistory.Messages, chatOptions)
                    .GetAwaiter()
                    .GetResult();
                return response;
            }
        }

        internal static string ExceptionDetails(this Exception ex, int level = 0) => ex == null ? string.Empty :
            $"{new string(' ', level * 2)}{ex.Message}{Environment.NewLine}{ex.InnerException.ExceptionDetails(level + 1)}".Trim();
    }
}