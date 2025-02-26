using Microsoft.Xrm.Sdk;
using System;
using System.IO;

namespace Rappen.XRM.RappSack
{
    public class RappSackConsole : RappSackCore
    {
        public string WorkFolder { get; }

        public RappSackConsole(IOrganizationService service, string workfolder, TraceTiming timing) : base(service, new RappSackConsoleTracer(workfolder, timing))
        {
            WorkFolder = workfolder.Contains(":\"") ? workfolder : Path.Combine(Path.GetTempPath(), workfolder);
        }

        public void Cmd(string args, string folder = null)
        {
            Trace($"Cmd: {args}");
            if (!string.IsNullOrEmpty(folder))
            {
                args = $"/c cd /d {folder} && {args}";
                Trace($"     in folder: {folder}");
            }
            System.Diagnostics.Process.Start("cmd.exe", args).WaitForExit();
            Trace("Cmd: Called");
        }
    }

    internal class RappSackConsoleTracer : RappSackTracerCore
    {
        private string logpath;

        public RappSackConsoleTracer(string workfolder, TraceTiming timing) : base(timing)
        {
            workfolder = workfolder.Contains(":\"") ? workfolder : Path.Combine(Path.GetTempPath(), workfolder);
            if (!Directory.Exists(workfolder))
            {
                Directory.CreateDirectory(workfolder);
            }
            var logfile = $"RappSackConsoleTracer_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            logpath = Path.Combine(workfolder, logfile);
            TraceInternal($"Created log file: {logpath}", "", 0);
        }

        protected override void TraceInternal(string message, string timestamp, int indent, TraceLevel level = TraceLevel.Information)
        {
            message = $"{timestamp}{new string(' ', indent * 2)}{message}";
            Console.WriteLine(message);
            try
            {
                File.AppendAllText(logpath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to {logpath}:{Environment.NewLine}{ex.Message}");
            }
        }
    }
}