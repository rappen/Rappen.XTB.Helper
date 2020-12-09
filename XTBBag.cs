using Microsoft.Xrm.Sdk;
using Rappen.XTB.Helpers.Interfaces;

namespace Rappen.XTB.Helpers
{
    public class XTBBag : IBag
    {
        public ILogger Logger { get; }

        public IOrganizationService Service { get; }

        public XTBBag(IOrganizationService service, ILogger logger)
        {
            Service = service;
            Logger = logger;
        }
    }
}
