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

            if (!string.IsNullOrWhiteSpace(introMessage))
            {
                if (!chatMessageHistory.Messages.Where(m => m.Role == ChatRole.System).Any()) //Only add system message once.
                {
                    chatMessageHistory.Add(ChatRole.System, introMessage, true);
                }           
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
                            using (var client = new AnthropicClient(apikey))
                            {
                                var chatClient = client.AsBuilder().ConfigureOptions(options =>
                                {
                                    options.ModelId = model.Name;
                                    options.MaxOutputTokens = 4096;
                                }).UseFunctionInvocation().Build();

                                var executeRequestDelegate = executeRequest;
                                var chatOptions = new ChatOptions
                                {
                                    Tools = new List<AITool>
                                    {
                                        AIFunctionFactory.Create(executeRequestDelegate)
                                    }
                                };

                                ChatResponse response = null;
                                try
                                {
                                    response = chatClient.GetResponseAsync(chatMessageHistory.Messages, chatOptions).Result;
                                }
                                catch (Exception ex)
                                {
                                    // Check for HTTP status code 529 (Anthropic service overloaded)
                                    var httpEx = ex as Anthropic.ApiException ?? ex.InnerException as Anthropic.ApiException;
                                    if (httpEx != null && (int) httpEx.StatusCode == 529)
                                    {
                                        throw new Exception("Anthropic service is overloaded, please try again later.");
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }

                                a.Result = response;
                              
                            }
                            break;

                        case "OpenAI":
                            throw new NotImplementedException();
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
                },
                
            });
        }
    }
}