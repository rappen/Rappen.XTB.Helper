﻿using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Interfaces;
using System;

namespace Rappen.XRM.Helpers.Plugin
{
    public class PluginBag : IBag, ITracingService, IDisposable
    {
        private readonly PluginLogger logger;

        public ILogger Logger => logger;

        public bool AddTimes { get { return logger.AddTimes; } set { logger.AddTimes = value; } }

        public IOrganizationService Service { get; }
        public IPluginExecutionContext5 Context { get; }
        public ContextEntity ContextEntity { get; }
        public ContextEntityCollection ContextEntityCollection { get; }

        public PluginBag(IServiceProvider serviceProvider)
        {
            logger = new PluginLogger(serviceProvider);
            Context = (IPluginExecutionContext5)serviceProvider.GetService(typeof(IPluginExecutionContext5));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            Service = serviceFactory.CreateOrganizationService(Context.InitiatingUserId);
            ContextEntity = new ContextEntity(Context);
            ContextEntityCollection = new ContextEntityCollection(Context);
        }

        public void Dispose()
        {
            logger?.Dispose();
        }

        public void Trace(string format, params object[] args) => logger.Trace(format, args);

        public void Cmd(string args, string folder = null)
        {
            throw new NotImplementedException();
        }
    }
}