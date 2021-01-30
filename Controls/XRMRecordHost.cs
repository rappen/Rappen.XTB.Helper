using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers.ControlItems;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public class XRMRecordHost : Component
    {
        #region Private Fields

        private List<IXRMRecordControl> controls;
        private Guid id = Guid.Empty;
        private string logicalName = null;
        private IOrganizationService organizationService;
        private Microsoft.Xrm.Sdk.AttributeCollection updatedattributes;
        private bool layoutsuspended;

        #endregion Private Fields

        #region Public Constructors

        public XRMRecordHost()
        {
            controls = new List<IXRMRecordControl>();
        }

        #endregion Public Constructors

        #region Public Properties

        [Browsable(false)]
        public Entity Record
        {
            get => EntityItem?.Entity;
            set
            {
                if (EntityItem?.Entity?.Id == value?.Id)
                {
                    return;
                }
                EntityItem = value != null ? new EntityItem(value, organizationService) : null;
                logicalName = value?.LogicalName;
                id = value?.Id ?? Guid.Empty;
                Refresh();
            }
        }

        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        [Category("Rappen XRM")]
        [DisplayName("Record Id")]
        [Description("Id of the record. LogicalName must be set before setting the Id.")]
        public Guid Id
        {
            get => id;
            set => SetId(value);
        }

        [Category("Rappen XRM")]
        [DisplayName("Record LogicalName")]
        [Description("LogicalName of the entity type to bind.")]
        public string LogicalName
        {
            get => logicalName;
            set => SetLogicalName(value);
        }

        [Browsable(false)]
        public IOrganizationService Service
        {
            get => organizationService;
            set
            {
                if (value == organizationService)
                {
                    return;
                }
                EntityItem = null;
                organizationService = value;
                LoadRecord();
                Refresh();
            }
        }

        #endregion Public Properties

        #region Internal Properties

        internal EntityItem EntityItem { get; private set; }

        internal EntityMetadata Metadata => EntityItem?.Metadata?.Metadata;

        internal bool Suspended => layoutsuspended;

        #endregion Internal Properties

        #region Public Methods

        public object this[string columnname]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(columnname))
                {
                    return null;
                }
                if (updatedattributes != null && updatedattributes.TryGetValue(columnname, out object value))
                {
                    return value;
                }
                if (Record != null && Record.Attributes.TryGetValue(columnname, out object valueorig))
                {
                    return valueorig;
                }
                return null;
            }
            set
            {
                if (updatedattributes == null)
                {
                    updatedattributes = new Microsoft.Xrm.Sdk.AttributeCollection();
                }
                if (Metadata.Attributes.Any(e => e.LogicalName == columnname))
                {
                    updatedattributes[columnname] = value;
                }
                else
                {
                    MessageBox.Show($"Column '{columnname}' is not available for table {Record.LogicalName}.", "Set value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public bool HasChanges()
        {
            if (updatedattributes == null)
            {
                return false;
            }
            // TODO Compare each updated attribute with associated Entity record.
            return updatedattributes.Count > 0;
        }

        public void Refresh()
        {
            if (layoutsuspended)
            {
                return;
            }
            foreach (var child in controls)
            {
                child.PopulateFromRecord();
            }
        }

        public bool SaveChanges()
        {
            if (Record == null)
            {
                throw new Exception("No assigned Record, cannot save.");
            }
            if (organizationService == null)
            {
                throw new Exception("No assigned Service, cannot save.");
            }
            if (updatedattributes?.Count() < 1)
            {
                return false;
            }
            var saverecord = new Entity(Record.LogicalName, Record.Id);
            saverecord.Attributes.AddRange(updatedattributes);
            if (saverecord.Id.Equals(Guid.Empty))
            {
                organizationService.Create(saverecord);
            }
            else
            {
                organizationService.Update(saverecord);
            }
            updatedattributes = null;
            return true;
        }

        public void SuspendLayout()
        {
            layoutsuspended = true;
        }

        public void ResumeLayout()
        {
            layoutsuspended = false;
            Refresh();
        }

        #endregion Public Methods

        #region Internal Methods

        internal void AddControl(IXRMRecordControl control)
        {
            if (!controls.Contains(control))
            {
                controls.Add(control);
            }
            Refresh();
        }

        internal void RemoveControl(IXRMRecordControl control)
        {
            if (controls.Contains(control))
            {
                controls.Remove(control);
            }
            Refresh();
        }

        #endregion Internal Methods

        #region Private Methods

        private void LoadRecord()
        {
            updatedattributes = null;
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

        private void SetId(Guid value)
        {
            if (value == id)
            {
                return;
            }
            id = string.IsNullOrWhiteSpace(logicalName) ? id = Guid.Empty : value;
            LoadRecord();
            Refresh();
        }

        private void SetLogicalName(string value)
        {
            if (value == logicalName)
            {
                return;
            }
            logicalName = value;
            id = Guid.Empty;
            EntityItem = null;
            Refresh();
        }

        #endregion Private Methods
    }
}