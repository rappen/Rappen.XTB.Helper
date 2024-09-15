using Microsoft.Xrm.Sdk;
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
        /// <summary>
        /// Generic way to load metadatas using XTB cache or old style.
        /// Can be called like this: this.GetAllEntityMetadatas(resulthandling, trycache, waittobedone, dontforce)
        /// </summary>
        /// <param name="plugin">The Plugin</param>
        /// <param name="loadedentities">Method taking EntityMetadata[] and a manuallycalled bool</param>
        /// <param name="TryMetadataCache">If we should try to use the XTB cache</param>
        /// <param name="WaitUntilMetadataLoaded">True to wait for the cache, false if doing it underhood</param>
        /// <param name="ForceReload">Set this to force get it from the Dataverse, not loaded cache</param>
        public static void GetAllEntityMetadatas(this PluginControlBase plugin, Action<IEnumerable<EntityMetadata>, bool> loadedentities, bool TryMetadataCache = true, bool WaitUntilMetadataLoaded = true, bool ForceReload = false)
        {
            if (plugin == null || plugin.Service == null || plugin.ConnectionDetail == null)
            {
                loadedentities?.Invoke(null, ForceReload);
                return;
            }
            plugin.WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading entities metadata...",
                Work = (worker, eventargs) =>
                {
                    if (TryMetadataCache && plugin.ConnectionDetail.MetadataCacheLoader != null)
                    {   // Try cache metadata
                        if (ForceReload)
                        {
                            worker.ReportProgress(0, "Reloading Metadata...");
                            plugin.ConnectionDetail.UpdateMetadataCache(true).ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        if (plugin.ConnectionDetail.MetadataCache != null)
                        {   // Already cached
                            worker.ReportProgress(0, "Get Metadata from cache...");
                            eventargs.Result = plugin.ConnectionDetail.MetadataCache;
                        }
                        else if (WaitUntilMetadataLoaded)
                        {   // Load the cache until done
                            worker.ReportProgress(0, "Reloading Metadata into the cache...");
                            eventargs.Result = plugin.ConnectionDetail.MetadataCacheLoader.ConfigureAwait(false).GetAwaiter().GetResult()?.EntityMetadata;
                        }
                        else
                        {   // Load the cache in background
                            worker.ReportProgress(0, "Reloaded Metadata into the cache in the background...");
                            plugin.ConnectionDetail.MetadataCacheLoader.ContinueWith(task =>
                            {   // Waiting for loaded
                                MethodInvoker mi = delegate
                                {
                                    loadedentities?.Invoke(task.Result?.EntityMetadata, ForceReload);
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
                        loadedentities?.Invoke(completedargs.Result as IEnumerable<EntityMetadata>, ForceReload);
                    }
                }
            });
        }

        internal static bool GetFriendlyAttributeIsNotAsRawValue(this IOrganizationService service, string entity, string attribute)
        => service.GetAttribute(entity, attribute).GetFriendlyAttributeIsNotAsRawValue();

        internal static bool GetFriendlyAttributeIsNotAsRawValue(this AttributeMetadata meta)
        {
            switch (meta?.AttributeType)
            {
                case AttributeTypeCode.Boolean:
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.DateTime:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.PartyList:
                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    return true;

                case AttributeTypeCode.Integer:
                    return meta.IsPOA();

                case AttributeTypeCode.Virtual:
                    if (meta is MultiSelectPicklistAttributeMetadata)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }
    }
}