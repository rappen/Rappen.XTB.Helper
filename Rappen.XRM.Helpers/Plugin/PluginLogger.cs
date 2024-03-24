using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;

namespace Rappen.XRM.Helpers.Plugin
{
    internal class PluginLogger : ILogger, ITracingService, IDisposable
    {
        private readonly ITracingService trace;
        private readonly Microsoft.Xrm.Sdk.PluginTelemetry.ILogger logger;
        private List<string> sections;
        public bool AddTimes { get; set; } = false;

        private string GetAddTime(bool force = false, bool fulldate = false) => AddTimes || force ? fulldate ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" : DateTime.Now.ToString("HH:mm:ss.fff") + "\t" : "";

        public PluginLogger(IServiceProvider serviceProvider)
        {
            trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            logger = (Microsoft.Xrm.Sdk.PluginTelemetry.ILogger)serviceProvider.GetService(typeof(Microsoft.Xrm.Sdk.PluginTelemetry.ILogger));
            sections = new List<string>();
            trace.Trace(GetAddTime(true, true));
        }

        public void Dispose()
        {
            if (sections.Count > 0)
            {
                trace.Trace("[PluginLogger] Ending unended blocks - check code consistency!");
                while (sections.Count > 0)
                {
                    EndSection();
                }
            }
            trace.Trace(GetAddTime(true, true));
        }

        public void EndSection()
        {
            if (sections.Count > 0)
            {
                sections.RemoveAt(sections.Count - 1);
            }
            Log("/");
        }

        public void Log(string message)
        {
            var indent = new string(' ', sections.Count * 2);
            trace.Trace(GetAddTime() + indent + message);
            logger.LogInformation(message);
        }

        public void Log(Exception ex)
        {
            trace.Trace(GetAddTime() + "Error:");
            trace.Trace(ex.Message);
            logger.LogError(ex, ex.Message);
        }

        public void StartSection(string name = null)
        {
            Log("\\ " + name);
            sections.Add(name);
        }

        public void Trace(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }
    }
}