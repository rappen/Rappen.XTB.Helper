using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Rappen.XTB.Helpers.Extensions
{
    public static class MetadataExtensions
    {
        public static void GetAllEntityMetadatas(this PluginControlBase plugin, Action<IEnumerable<EntityMetadata>> loadedentities, bool TryMetadataCache = true, bool WaitUntilMetadataLoaded = true)
        {
            if (plugin == null || plugin.Service == null || plugin.ConnectionDetail == null)
            {
                loadedentities?.Invoke(null);
                return;
            }
            plugin.WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading entities metadata...",
                Work = (worker, eventargs) =>
                {
                    if (TryMetadataCache && plugin.ConnectionDetail.MetadataCacheLoader != null)
                    {   // Try cache metadata
                        if (plugin.ConnectionDetail.MetadataCache != null)
                        {   // Already cached
                            worker.ReportProgress(0, "Get Metadata from cache...");
                            eventargs.Result = plugin.ConnectionDetail.MetadataCache;
                        }
                        else if (WaitUntilMetadataLoaded)
                        {   // Load the cache until done
                            worker.ReportProgress(0, "Reloaded Metadata into the cache...");
                            eventargs.Result = plugin.ConnectionDetail.MetadataCacheLoader.ConfigureAwait(false).GetAwaiter().GetResult()?.EntityMetadata;
                        }
                        else
                        {   // Load the cache in background
                            worker.ReportProgress(0, "Reloaded Metadata into the cache in the background...");
                            plugin.ConnectionDetail.MetadataCacheLoader.ContinueWith(task =>
                            {   // Waiting for loaded
                                MethodInvoker mi = delegate
                                {
                                    loadedentities?.Invoke(task.Result?.EntityMetadata);
                                };
                                if (plugin.InvokeRequired)
                                {
                                    plugin.Invoke(mi);
                                }
                                else
                                {
                                    mi();
                                }
                            });
                        }
                    }
                    else
                    {   // Load as usual, the old way
                        worker.ReportProgress(0, "Loading Metadata from database...");
                        eventargs.Result = plugin.Service.LoadEntities(plugin.ConnectionDetail.OrganizationMajorVersion, plugin.ConnectionDetail.OrganizationMinorVersion).EntityMetadata;
                    }
                },
                ProgressChanged = (changeargs) =>
                {
                    plugin.SetWorkingMessage(changeargs.UserState.ToString());
                },
                PostWorkCallBack = (completedargs) =>
                {
                    if (completedargs.Error != null)
                    {
                        plugin.ShowErrorDialog(completedargs.Error, "Load Entities");
                    }
                    else
                    {
                        loadedentities?.Invoke(completedargs.Result as IEnumerable<EntityMetadata>);
                    }
                }
            });
        }
    }
}