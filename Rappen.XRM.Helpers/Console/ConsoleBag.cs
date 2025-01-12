using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Interfaces;
using System.IO;

namespace Rappen.XRM.Helpers.Console
{
    public class ConsoleBag : IBag
    {
        private ConsoleLogger logger;

        public ILogger Logger => logger;

        public readonly string WorkingFolder;

        public IOrganizationService Service { get; }

        public ConsoleBag(IOrganizationService service, string rootfolder)
        {
            WorkingFolder = rootfolder.Contains(":\"") ? rootfolder : Path.Combine(Path.GetTempPath(), rootfolder);
            Service = service;
            logger = new ConsoleLogger(WorkingFolder);
        }

        public void Trace(string format, params object[] args) => logger.Log(string.Format(format, args));

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
}