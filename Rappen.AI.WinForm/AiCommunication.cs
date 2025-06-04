using Anthropic;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                Work = async (w, a) =>
                {
                    switch (supplier.Name)
                    {
                        case "Anthropic":
                            AskAnthropic(model, apikey, chatMessageHistory, executeRequest, a);
                            break;

                        case "OpenAI":
                            AskOpenAI(model, apikey, chatMessageHistory, executeRequest, a);
                            break;
                    }
                },
                PostWorkCallBack = (w) =>
                {
                    tool.Cursor = Cursors.Default;
                    if (w.Error != null)
                    {
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

        private static void AskAnthropic(AiModel model, string apikey, ChatMessageHistory chatMessageHistory, Func<string, string> executeRequest, System.ComponentModel.DoWorkEventArgs workEventArgs)
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

                ChatResponse response = null;
                try
                {
                    response = chatClient
                        .GetResponseAsync(chatMessageHistory.Messages, chatOptions)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    var httpEx = ex as Anthropic.ApiException ?? ex.InnerException as Anthropic.ApiException;
                    if (httpEx == null)
                    {
                        throw;
                    }
                    if (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new Exception("ApiKey may be incorrect.");
                    }
                    // Check for HTTP status code 529 (Anthropic service overloaded)
                    if ((int)httpEx.StatusCode == 529)
                    {
                        throw new Exception("Anthropic service is overloaded, please try again later.");
                    }
                }
                workEventArgs.Result = response;
            }
        }
        private static void AskOpenAI(AiModel model, string apikey, ChatMessageHistory chatMessageHistory, Func<string, string> executeRequest, DoWorkEventArgs a)
        {
            throw new NotImplementedException();
        }
    }
}