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

        public virtual ServiceAs ServiceAs { get; } = ServiceAs.User;
        public virtual string ExecuterEnvVar { get; } = string.Empty;

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                serviceProvider.TraceContext(false, true, false, false, true);
                Context = serviceProvider.Get<IPluginExecutionContext5>();
                ContextEntity = new ContextEntity(Context);
                ContextEntityCollection = new ContextEntityCollection(Context);
                SetTracer(new RappSackPluginTracer(serviceProvider));
                Guid? svcuser = null;
                switch (ServiceAs)
                {
                    case ServiceAs.Initiating:
                        svcuser = Context.InitiatingUserId;
                        break;

                    case ServiceAs.User:
                        svcuser = Context.UserId;
                        break;

                    case ServiceAs.Specific:
                        if (string.IsNullOrWhiteSpace(ExecuterEnvVar))
                        {
                            throw new InvalidPluginExecutionException("ServiceAs is Specific, but ExecuterEnvVar is not set");
                        }
                        SetService(serviceProvider, null);
                        svcuser = GetEnvironmentVariableValue<Guid>(ExecuterEnvVar, true);
                        break;
                }
                SetService(serviceProvider, svcuser);
                var starttime = DateTime.Now;
                TraceRaw($"Execution {CallerMethodName() ?? "RappSackPlugin"} at {starttime:yyyy-MM-dd HH:mm:ss.fff}");
                Execute();
                TraceRaw($"Exiting after {(DateTime.Now - starttime).ToSmartString()}");
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

        private void SetService(IServiceProvider provider, Guid? userid) => SetService(provider.Get<IOrganizationServiceFactory>(), userid);
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

        protected override void TraceInternal(string message, string timestamp, int indent, TraceLevel level = TraceLevel.Information)
        {
            if (tracing == null)
            {
                throw new InvalidPluginExecutionException("Tracer is not initialized");
            }
            tracing.Trace(timestamp + new string(' ', indent * 2) + message);
        }
    }

    public enum ServiceAs
    {
        User,
        Initiating,
        System,
        Specific
    }
}