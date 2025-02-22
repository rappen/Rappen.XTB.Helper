using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Rappen.Dataverse.Canary;
using System;

namespace Rappen.XRM.RappSack
{
    public abstract class RappSackPlugin : RappSackCore, IPlugin, ITracingService
    {
        public IPluginExecutionContext5 Context { get; private set; }
        public ContextEntity ContextEntity { get; private set; }
        public ContextEntityCollection ContextEntityCollection { get; private set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                serviceProvider.TraceContext(false, true, false, false, true);
                SetService(serviceProvider.Get<IOrganizationService>());
                SetTracer(new RappSackPluginTracer(serviceProvider));
                Context = serviceProvider.Get<IPluginExecutionContext5>();
                ContextEntity = new ContextEntity(Context);
                ContextEntityCollection = new ContextEntityCollection(Context);
                try
                {
                    Execute();
                }
                catch (Exception e)
                {
                    Trace(e);
                }
            }
            catch (Exception ex)
            {
                serviceProvider.TraceError(ex);
                if (ex is InvalidPluginExecutionException)
                {
                    throw;
                }
                throw new InvalidPluginExecutionException($"Unhandled {ex.GetType()} in RappSackPlugin: {ex.Message}", ex);
            }
        }

        public abstract void Execute();

        public void Trace(string format, params object[] args) => base.Trace(string.Format(format, args));
    }

    internal class RappSackPluginTracer : RappSackTracerCore
    {
        private readonly ITracingService tracing;

        public RappSackPluginTracer(IServiceProvider provider) : base(TraceTiming.ElapsedSinceLast)
        {
            if (!(provider.GetService(typeof(ITracingService)) is ITracingService tracingService))
            {
                throw new InvalidPluginExecutionException("Failed to get tracing service");
            }
            tracing = tracingService;
        }

        protected override void InternalTrace(string message, string timestamp, int indent, TraceLevel level = TraceLevel.Information) => tracing.Trace(timestamp + new string(' ', indent * 2) + message);
    }
}