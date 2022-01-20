using Microsoft.Xrm.Sdk;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.Collections.Generic;

namespace Rappen.XRM.Helpers.Plugin
{
    internal class PluginLogger : ILogger, ITracingService, IDisposable
    {
        private readonly ITracingService trace;
        private List<string> sections;

        public PluginLogger(IServiceProvider serviceProvider)
        {
            trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            sections = new List<string>();
            Log(DateTime.Now.ToString("yyyy-MM-dd"));
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
            trace.Trace("*** Exit");
        }

        public void EndSection()
        {
            if (sections.Count > 0)
            {
                sections.RemoveAt(sections.Count - 1);
            }
            Log("⤴");
        }

        public void Log(string message)
        {
            var indent = new string(' ', sections.Count * 2);
            trace.Trace(DateTime.Now.ToString("HH:mm:ss.fff") + "\t" + indent + message);
        }

        public void Log(Exception ex)
        {
            trace.Trace("Error:");
            trace.Trace(ex.Message);
        }

        public void StartSection(string name = null)
        {
            Log("⤵ " + name);
            sections.Add(name);
        }

        public void Trace(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }
    }
}
