using Microsoft.Xrm.Sdk;
using System;

namespace Rappen.XTB.Helpers.Interfaces
{
    public interface IXRMRecordControl
    {
        string LogicalName { get; set; }
        Guid Id { get; set; }
        IOrganizationService Service { get; set; }
        EntityReference EntityReference { get; set; }
        Entity Entity { get; set; }

        void Refresh();
    }
}