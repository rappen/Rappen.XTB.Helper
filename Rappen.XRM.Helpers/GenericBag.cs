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

        public void Cmd(string args, string folder = null)
        {
            throw new System.NotImplementedException();
        }

        public void Trace(string format, params object[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}