using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers.ControlItems;
using Rappen.XTB.Helpers.Interfaces;
using System;

namespace Rappen.XTB.Helpers.Controls
{
    public class XRMRecordControlBase
    {
        private string logicalName = null;
        private Guid id = Guid.Empty;
        private IOrganizationService organizationService;
        private IXRMRecordControl control;

        public XRMRecordControlBase(IXRMRecordControl control)
        {
            this.control = control;
        }

        internal EntityItem EntityItem { get; private set; }

        internal string GetLogicalName() => logicalName;

        internal void SetLogicalName(string value)
        {
            if (value != logicalName)
            {
                logicalName = value;
                id = Guid.Empty;
                EntityItem = null;
                control.Refresh();
            }
        }

        internal Guid GetId() => id;

        internal void SetId(Guid value)
        {
            if (!value.Equals(id))
            {
                id = string.IsNullOrWhiteSpace(logicalName) ? id = Guid.Empty : value;
                LoadRecord();
                control.Refresh();
            }
        }

        internal IOrganizationService GetService() => organizationService;

        internal void SetService(IOrganizationService value)
        {
            if (value != organizationService)
            {
                EntityItem = null;
                organizationService = value;
                LoadRecord();
                control.Refresh();
            }
        }

        internal EntityReference GetEntityReference()
        {
            var result = EntityItem?.Entity?.ToEntityReference();
            if (result == null)
            {
                return null;
            }
            result.Name = new EntityItem(EntityItem?.Entity, organizationService).ToString();
            return result;
        }

        internal void SetEntityReference(EntityReference value)
        {
            if (value?.LogicalName != logicalName || value?.Id.Equals(id) != true)
            {
                SetLogicalName(value?.LogicalName);
                SetId(value?.Id ?? Guid.Empty);
                control.Refresh();
            }
        }

        internal Entity GetEntity() => EntityItem?.Entity;

        internal void SetEntity(Entity value)
        {
            if (EntityItem?.Entity?.Id.Equals(value?.Id) != true)
            {
                EntityItem = value != null ? new EntityItem(value, organizationService) : null;
                logicalName = value?.LogicalName;
                id = value?.Id ?? Guid.Empty;
                control.Refresh();
            }
        }

        private void LoadRecord()
        {
            if (organizationService != null && !string.IsNullOrWhiteSpace(logicalName) && !Guid.Empty.Equals(id))
            {
                var record = organizationService.Retrieve(logicalName, id, new ColumnSet(true));
                EntityItem = new EntityItem(record, organizationService);
            }
            else
            {
                EntityItem = null;
            }
        }
    }
}