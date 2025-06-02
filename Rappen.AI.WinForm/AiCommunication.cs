using Anthropic;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Rappen.AI.WinForm
{
    public static class AiCommunication
    {
        public static void CallingAI(string prompt, string introMessage, AiSupplier supplier, AiModel model, string apikey, ChatMessageHistory chatMessageHistory, PluginControlBase tool, Action<string> handleResult, Func<string, string> executeFetchXmlRequest)
        {
            tool.Cursor = Cursors.WaitCursor;

            if (!string.IsNullOrWhiteSpace(introMessage))
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
                            using (var client = new AnthropicClient(apikey))
                            {
                                var chatClient = client.AsBuilder().ConfigureOptions(options =>
                                {
                                    options.ModelId = model.Name;
                                    options.MaxOutputTokens = 4096;
                                }).UseFunctionInvocation().Build();

                                var executeFetchXmlRequestDelegate = executeFetchXmlRequest;
                                var chatOptions = new ChatOptions
                                {
                                    Tools = new List<AITool>
                                    {
                                        AIFunctionFactory.Create(executeFetchXmlRequestDelegate)
                                    }
                                };

                                var response = chatClient.GetResponseAsync(chatMessageHistory.Messages, chatOptions);
                                a.Result = response.Result;
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
                        handleResult?.Invoke(response.ToString());
                    }
                }
            });
        }
    }
}