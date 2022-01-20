using Microsoft.Xrm.Sdk;
using Rappen.CDS.Canary;
using System;
using System.Diagnostics;

namespace Rappen.XRM.Helpers.Plugin
{
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            using (var bag = new PluginBag(serviceProvider))
            {
                var watch = Stopwatch.StartNew();
                try
                {
                    bag.TraceContext(bag.Context);
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

        public abstract void Execute(PluginBag bag);
    }
}
