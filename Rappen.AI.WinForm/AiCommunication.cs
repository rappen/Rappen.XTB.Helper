using Anthropic;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Rappen.AI.WinForm
{
    public static class AiCommunication
    {
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

        private static ChatResponse AskAI(string supplier, string model, string apikey, ChatMessageHistory chatMessageHistory, params Func<string, string>[] internalTools)
        {
            using (IChatClient client =
                supplier == "Anthropic" ? new AnthropicClient(apikey) :
                supplier == "OpenAI" ? new ChatClient(model, apikey).AsIChatClient() :
                throw new NotImplementedException($"AI Supplier {supplier} not implemented!"))
            {
                var chatClient = client.AsBuilder().ConfigureOptions(options =>
                {
                    options.ModelId = model;
                    options.MaxOutputTokens = 4096;
                }).UseFunctionInvocation().Build();

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

        internal static string ExceptionDetails(this Exception ex, int level = 0) => ex == null ? string.Empty :
            $"{new string(' ', level * 2)}{ex.Message}{Environment.NewLine}{ex.InnerException.ExceptionDetails(level + 1)}".Trim();
    }
}