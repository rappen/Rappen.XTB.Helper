using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Interfaces;

namespace Rappen.XRM.Helpers
{
    public class GenericBag : IBag
    {
        public ILogger Logger { get; }

        public IOrganizationService Service { get; }

        public GenericBag(IOrganizationService service)
        {
            Service = service;
            Logger = new VoidLogger();
        }
    }
}
