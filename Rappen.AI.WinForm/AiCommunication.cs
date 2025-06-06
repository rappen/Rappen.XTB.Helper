using Anthropic;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Rappen.AI.WinForm
{
    public static class AiCommunication
    {
        public static void CallingAI(string prompt, string introMessage, AiSupplier supplier, AiModel model, string apikey, ChatMessageHistory chatMessageHistory, PluginControlBase tool, Func<string, string> executeRequest, Action<string> handleResponse)
        {
            tool.Cursor = Cursors.WaitCursor;

            if (!string.IsNullOrWhiteSpace(introMessage) &&
                !chatMessageHistory.Messages.Where(m => m.Role == ChatRole.System).Any()) //Only add system message once.
            {
                chatMessageHistory.Add(ChatRole.System, introMessage, true);
            }

            chatMessageHistory.Add(ChatRole.User, prompt, false);

            tool.WorkAsync(new WorkAsyncInfo
            {
                Message = $"Asking the {supplier.Name}...",
                Work = (w, a) =>
                {
                    switch (supplier.Name)
                    {
                        case "Anthropic":
                            a.Result = AskAnthropic(model, apikey, chatMessageHistory, executeRequest);
                            break;

                        case "OpenAI":
                            a.Result = AskOpenAI(model, apikey, chatMessageHistory, executeRequest);
                            break;
                    }
                },
                PostWorkCallBack = (w) =>
                {
                    tool.Cursor = Cursors.Default;
                    if (w.Error != null)
                    {
                        var apiEx = w.Error as ApiException ?? w.Error.InnerException as ApiException;
                        if (apiEx != null)
                        {
                            if (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                tool.ShowErrorDialog(new Exception("ApiKey may be incorrect."));
                                return;
                            }
                            else if ((int)apiEx.StatusCode == 529) // Anthropic service overloaded
                            {
                                tool.ShowErrorDialog(new Exception("Anthropic service is overloaded, please try again later."));
                                return;
                            }
                        }
                        tool.ShowErrorDialog(w.Error);
                    }
                    else if (w.Result is ChatResponse response)
                    {
                        chatMessageHistory.Add(response);
                        handleResponse?.Invoke(response.ToString());
                    }
                }
            });
        }

        private static ChatResponse AskAnthropic(AiModel model, string apikey, ChatMessageHistory chatMessageHistory, Func<string, string> executeRequest)
        {
            using (var client = new AnthropicClient(apikey))
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
        private static object AskOpenAI(AiModel model, string apikey, ChatMessageHistory chatMessageHistory, Func<string, string> executeRequest)
        {
            throw new NotImplementedException();
        }
    }
}