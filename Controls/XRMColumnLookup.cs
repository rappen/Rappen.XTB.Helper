using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers.ControlItems;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel.Configuration;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMColumnLookup : ComboBox, IXRMRecordControl
    {
        private string displayFormat = string.Empty;
        private bool onlyactiverecords = true;
        private IEnumerable<Entity> records;
        private IOrganizationService organizationService;
        private XRMRecordHost recordhost;
        private string column;
        private string loadedentityname;
        private string loadedcolumnname;
        private bool sorted = false;
        private bool populating;

        public XRMColumnLookup()
        {
            InitializeComponent();
            SelectedIndexChanged += XRMColumnLookup_SelectedIndexChanged;
        }

        private void XRMColumnLookup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DesignMode || populating || recordhost == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(column))
            {
                MessageBox.Show("Cannot set value, Column property missing.", this.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            recordhost[column] = SelectedRecord != null ? SelectedRecord.ToEntityReference() : null;
        }

        [Category("Rappen XRM")]
        public XRMRecordHost RecordHost
        {
            get => recordhost;
            set
            {
                if (recordhost == value)
                {
                    return;
                }
                recordhost?.RemoveControl(this);
                recordhost = value;
                recordhost?.AddControl(this);
                if (recordhost?.Service != null)
                {
                    organizationService = recordhost.Service;
                }
                if (recordhost != null && !string.IsNullOrWhiteSpace(column))
                {
                    PopulateFromRecord();
                }
                else
                {
                    Clear();
                }
            }
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
                organizationService = value;
                if (organizationService != null && records?.Count() > 0)
                {
                    PopulateRecords();
                }
                else
                {
                    Clear();
                }
            }
        }

        [Category("Rappen XRM")]
        [Description("Indicates the source of data (EntityCollection) for the XRMColumnLookup control.")]
        [Browsable(false)]
        public new object DataSource
        {
            get
            {
                if (records != null)
                {
                    return records;
                }
                return base.DataSource;
            }
            set
            {
                if (value is EntityCollection entityCollection)
                {
                    records = entityCollection.Entities;
                }
                else if (value is IEnumerable<Entity> newentities)
                {
                    records = newentities;
                }
                else
                {
                    records = null;
                }
                if (records?.Count() > 0)
                {
                    PopulateRecords();
                }
                else
                {
                    Clear();
                }
            }
        }

        [Category("Rappen XRM")]
        [DisplayName("Display Format")]
        [Description("Single attribute from datasource to display for items, or use XRM Tokens syntax freely https://jonasr.app/xrm-tokens/")]
        public string DisplayFormat
        {
            get { return displayFormat; }
            set
            {
                if (value == displayFormat)
                {
                    return;
                }
                displayFormat = value;
                PopulateRecords();
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("Defines if the date should be sorted alphabetically, based on selected format")]
        public new bool Sorted
        {
            get { return sorted; }
            set
            {
                base.Sorted = false;
                if (value != sorted)
                {
                    sorted = value;
                    PopulateRecords();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("Defines if lookup records should be filtered by statecode=0")]
        public bool OnlyActiveRecords
        {
            get { return onlyactiverecords; }
            set
            {
                if (value != onlyactiverecords)
                {
                    onlyactiverecords = value;
                    GetRecordsFromRecordHost();
                }
            }
        }

        [Category("Rappen XRM")]
        [Description("Lookup column to bind the ComboBox to, to load available records as items.")]
        public string Column
        {
            get { return column; }
            set
            {
                if (value == column)
                {
                    return;
                }
                column = value;
                if (recordhost?.Record != null && !string.IsNullOrWhiteSpace(column))
                {
                    PopulateFromRecord();
                }
                else
                {
                    Clear();
                }
            }
        }

        private void GetRecordsFromRecordHost()
        {
            if (DesignMode
                || recordhost?.Record == null
                || string.IsNullOrWhiteSpace(column)
                || !(recordhost.Metadata?.Attributes.FirstOrDefault(a => a.LogicalName == column) is LookupAttributeMetadata lkp))
            {
                Clear();
                return;
            }
            if (lkp.Targets.Length != 1)
            {
                Clear();
                throw new XRMControlException(this, $"Cannot get records for {lkp.Targets.Length} targets.");
            }
            var query = new QueryExpression(lkp.Targets[0]);
            query.ColumnSet.AllColumns = true;
            if (onlyactiverecords)
            {
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            }
            DataSource = recordhost.Service.RetrieveMultiple(query);
            loadedentityname = recordhost.Metadata.LogicalName;
            loadedcolumnname = column;
        }

        private void Clear()
        {
            records = null;
            loadedentityname = null;
            loadedcolumnname = null;
            base.DataSource = null;
        }

        [Browsable(false)]
        public Entity SelectedRecord => (SelectedItem is EntityItem item) ? item.Entity : null;

        public void PopulateFromRecord()
        {
            if (DesignMode || recordhost == null)
            {
                return;
            }
            populating = true;
            if (recordhost?.Metadata?.LogicalName != loadedentityname || column != loadedcolumnname)
            {
                GetRecordsFromRecordHost();
            }
            if (recordhost?[column] is EntityReference entref)
            {
                SelectById(entref.Id);
            }
            populating = false;
        }

        public void PopulateRecords()
        {
            if (DesignMode || (organizationService == null && recordhost?.Service == null))
            {
                return;
            }
            var selected = SelectedRecord;
            var ds = records?.Select(r => new EntityItem(r, displayFormat, organizationService ?? recordhost?.Service)).ToArray();
            if (sorted && ds?.Length > 0)
            {
                ds = ds.OrderBy(r => r.ToString()).ToArray();
            }
            base.DataSource = ds;
            base.Refresh();
            SelectById(selected?.Id);
        }

        private void SelectById(Guid? id)
        {
            if (id != null && !id.Equals(Guid.Empty))
            {
                if (base.DataSource is IEnumerable<EntityItem> ds &&
                    ds?.FirstOrDefault(r => r.Entity.Id.Equals(id)) is EntityItem newselected)
                {
                    SelectedItem = newselected;
                }
                else
                {
                    SelectedIndex = -1;
                }
            }
        }
    }
}