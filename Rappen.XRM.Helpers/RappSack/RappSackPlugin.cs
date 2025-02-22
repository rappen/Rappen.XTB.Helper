using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Rappen.Dataverse.Canary;
using Rappen.XRM.Helpers.Plugin;
using System;

namespace Rappen.XRM.Helpers.RappSack
{
    public class RappSackPlugin : RappSackCore
    {
        public IPluginExecutionContext5 Context { get; }
        public ContextEntity ContextEntity { get; }
        public ContextEntityCollection ContextEntityCollection { get; }

        public RappSackPlugin(IServiceProvider provider, RappSackTracerCore tracer) : base(provider.Get<IOrganizationService>(), tracer)
        {
            Context = provider.Get<IPluginExecutionContext5>();
            ContextEntity = new ContextEntity(Context);
            ContextEntityCollection = new ContextEntityCollection(Context);
        }
    }

    public abstract class RappSackPluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                serviceProvider.TraceContext(false, true, false, false, true);
                var rappsack = new RappSackPlugin(serviceProvider, new RappSackPluginTracer(serviceProvider));
                try
                {
                    Execute(rappsack);
                }
                catch (Exception e)
                {
                    rappsack.Trace(e);
                }
            }
            catch (Exception ex)
            {
                serviceProvider.TraceError(ex);
                if (ex is InvalidPluginExecutionException)
                {
                    throw;
                }
                throw new InvalidPluginExecutionException($"Unhandled {ex.GetType()} in RappSackPluginBase: {ex.Message}", ex);
            }
        }

        public abstract void Execute(RappSackPlugin rappsack);
    }

    internal class RappSackPluginTracer : RappSackTracerCore
    {
        private readonly ITracingService trace;

        public RappSackPluginTracer(IServiceProvider provider) : base(TraceTiming.ElapsedSinceLast)
        {
            if (!(provider.GetService(typeof(ITracingService)) is ITracingService tracingService))
            {
                throw new InvalidPluginExecutionException("Failed to get tracing service");
            }
            trace = tracingService;
        }

        protected override void InternalTrace(string message, string timestamp, int indent, TraceLevel level = TraceLevel.Information) => trace.Trace(timestamp + new string(' ', indent * 2) + message);
    }
}