using Microsoft.Xrm.Sdk;
using Rappen.XTB.Helpers.Interfaces;
using System;

namespace Rappen.XRM.Helpers.Plugin
{
    public class PluginBag : IBag, ITracingService, IDisposable
    {
        private readonly PluginLogger logger;

        public ILogger Logger => logger;

        public IOrganizationService Service { get; }

        public IPluginExecutionContext Context { get; }

        public PluginBag(IServiceProvider serviceProvider)
        {
            logger = new PluginLogger(serviceProvider);
            Context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            Service = serviceFactory.CreateOrganizationService(Context.InitiatingUserId);
        }

        public void Dispose()
        {
            logger?.Dispose();
        }

        public void Trace(string format, params object[] args) => logger.Trace(format, args);
    }
}
