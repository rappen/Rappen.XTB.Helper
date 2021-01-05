using Microsoft.Xrm.Sdk;
using Rappen.XTB.Helpers.Interfaces;

namespace Rappen.XTB.Helpers
{
    public class GenericBag : IBag
    {
        public ILogger Logger { get; }

        public IOrganizationService Service { get; }

        public GenericBag(IOrganizationService service, ILogger logger)
        {
            Service = service;
            Logger = logger;
        }
    }
}
