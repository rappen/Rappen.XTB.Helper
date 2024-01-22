using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XTB.Helpers.ControlItems;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
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
                EntityItem = value != null ? new EntityItem(value, organizationService) : null;
                logicalName = value?.LogicalName;
                id = value?.Id ?? Guid.Empty;
                updatedattributes = null;
                Refresh();
            }
        }

        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        [Category("Rappen XRM")]
        [Description("Id of the record. LogicalName must be set before setting the Id.")]
        public Guid Id
        {
            get => id;
            set => SetId(value);
        }

        [Category("Rappen XRM")]
        [Description("LogicalName of the entity type to bind.")]
        public string LogicalName
        {
            get => logicalName;
            set => SetLogicalName(value);
        }

        [DefaultValue(false)]
        [Category("Rappen XRM")]
        [Description("Don't throw exception if LogicalName + Id doesn't exist in Dataverse.")]
        public bool NoExceptionByNonexistingId { get; set; } = false;

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

        public IEnumerable<string> ChangedColumns => updatedattributes?.Keys;

        internal object this[string columnname]
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
                if (Metadata == null)
                {
                    return;
                }
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
                var oldval = Record?.PropertyAsBaseType(columnname, null, true);
                var newval = EntityExtensions.AttributeToBaseType(value);
                if ((oldval == null && newval == null) || oldval?.Equals(newval) == true)
                {
                    updatedattributes.Remove(columnname);
                }
                AnnounceColumnChange(columnname);
            }
        }

        #endregion Public Properties

        [Category("Rappen XRM")]
        [Description("Called when any bound column control updates a column value on bound record.")]
        public event XRMRecordEventHandler ColumnValueChanged;

        #region Internal Properties

        internal EntityItem EntityItem { get; private set; }

        internal EntityMetadata Metadata => EntityItem?.Metadata?.Metadata;

        internal bool Suspended { get; private set; }

        #endregion Internal Properties

        #region Public Methods

        public void Refresh()
        {
            if (Suspended)
            {
                return;
            }
            foreach (var child in controls)
            {
                child.RecordUpdated();
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
            if (updatedattributes == null || updatedattributes?.Count() < 1)
            {
                return false;
            }
            var saverecord = new Entity(Record.LogicalName, Record.Id);
            saverecord.Attributes.AddRange(updatedattributes);
            if (saverecord.Id.Equals(Guid.Empty))
            {
                Record.Id = organizationService.Create(saverecord);
            }
            else
            {
                organizationService.Update(saverecord);
            }
            updatedattributes.ToList().ForEach(u => Record[u.Key] = u.Value);
            updatedattributes = null;
            AnnounceColumnChange(string.Empty);
            return true;
        }

        public void SetValue(string columnname, object value)
        {
            this[columnname] = value;
            controls.Where(c => c.Column.Equals(columnname)).ToList().ForEach(c => c.RecordUpdated());
        }

        public void UndoChanges()
        {
            updatedattributes = null;
            Refresh();
            AnnounceColumnChange(string.Empty);
        }

        public void SuspendLayout()
        {
            Suspended = true;
        }

        public void ResumeLayout()
        {
            Suspended = false;
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

        private void AnnounceColumnChange(string columnname)
        {
            new XRMRecordEventArgs(Record, columnname).OnRecordEvent(this, ColumnValueChanged);
        }

        private void LoadRecord()
        {
            updatedattributes = null;
            if (organizationService != null && !string.IsNullOrWhiteSpace(logicalName) && !Guid.Empty.Equals(id))
            {
                try
                {
                    var record = organizationService.Retrieve(logicalName, id, new ColumnSet(true));
                    EntityItem = new EntityItem(record, organizationService);
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    if (NoExceptionByNonexistingId && ex.HResult == -2146233087)
                    {
                        Id = Guid.Empty;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                EntityItem = null;
            }
            AnnounceColumnChange(string.Empty);
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