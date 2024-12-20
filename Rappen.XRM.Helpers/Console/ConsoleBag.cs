using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Interfaces;

namespace Rappen.XRM.Helpers.Console
{
    public class ConsoleBag : IBag
    {
        private ConsoleLogger logger;

        public ILogger Logger => logger;

        public IOrganizationService Service { get; }

        public ConsoleBag(IOrganizationService service)
        {
            Service = service;
            logger = new ConsoleLogger();
        }
        public void Trace(string format, params object[] args) => logger.Log(string.Format(format, args));
    }
}