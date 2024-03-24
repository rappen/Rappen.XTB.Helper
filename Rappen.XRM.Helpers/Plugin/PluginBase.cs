using Microsoft.Xrm.Sdk;
using Rappen.Dataverse.Canary;
using System;
using System.Diagnostics;

namespace Rappen.XRM.Helpers.Plugin
{
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                serviceProvider.TraceContext();
                using (var bag = new PluginBag(serviceProvider))
                {
                    var watch = Stopwatch.StartNew();
                    try
                    {
                        Execute(bag);
                    }
                    catch (Exception e)
                    {
                        bag.Logger.Log(e);
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        bag.Trace("Internal execution time: {0} ms", watch.ElapsedMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                serviceProvider.TraceError(ex);
                if (ex is InvalidPluginExecutionException)
                {
                    throw;
                }
                throw new InvalidPluginExecutionException($"Unhandled {ex.GetType()} in PluginBase: {ex.Message}", ex);
            }
        }

        public abstract void Execute(PluginBag bag);
    }
}