using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.ControlItems;
using Rappen.XTB.Helpers.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMColumnOptionSet : ComboBox, IXRMRecordControl
    {
        private bool showValue = true;
        private bool sorted = false;
        private IEnumerable<OptionMetadata> options;
        private XRMRecordHost recordhost;
        private string entityname;
        private string column;
        private bool populating;
        private bool addnulloption;

        public XRMColumnOptionSet()
        {
            InitializeComponent();
            SelectedIndexChanged += XRMColumnOptionSet_SelectedIndexChanged;
        }

        private void XRMColumnOptionSet_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (DesignMode || populating || recordhost == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(column))
            {
                MessageBoxEx.Show(this, "Cannot set value, Column property missing.", this.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            recordhost[column] = SelectedOption != null ? new OptionSetValue((int)SelectedOption.Value) : null;
        }

        [Category("Rappen XRM")]
        [Description("Indicates the source of data (OptionSetMetadata or collection of OptionMetadata) for the XRMOptionSetComboBox control.")]
        [Browsable(false)]
        public new object DataSource
        {
            get
            {
                if (options != null)
                {
                    return options;
                }
                return base.DataSource;
            }
            set
            {
                if (value is OptionSetMetadata osmeta)
                {
                    options = osmeta.Options;
                }
                else if (value is IEnumerable<OptionMetadata> newoptions)
                {
                    options = newoptions;
                }
                else
                {
                    options = null;
                }
                PopulateOptions();
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("True to show optionset value.")]
        public bool ShowValue
        {
            get { return showValue; }
            set
            {
                if (value != showValue)
                {
                    showValue = value;
                    PopulateOptions();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("Defines if the options should be sorted alphabetically, based on selected layout")]
        public new bool Sorted
        {
            get { return sorted; }
            set
            {
                base.Sorted = false;
                if (value != sorted)
                {
                    sorted = value;
                    PopulateOptions();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("Defines if a blank (null) option should be added at the top of the list of options.")]
        public bool AddNullOption
        {
            get { return addnulloption; }
            set
            {
                if (value != addnulloption)
                {
                    addnulloption = value;
                    PopulateOptions();
                }
            }
        }

        [Browsable(false)]
        public OptionMetadata SelectedOption => (SelectedItem is OptionMetadataItem item) ? item.Metadata : null;

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
            }
        }

        [Category("Rappen XRM")]
        [Description("OptionSet column to bind the ComboBox to.")]
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
                GetOptions();
                RecordUpdated();
            }
        }

        private void GetOptions()
        {
            if (DesignMode || recordhost == null || recordhost?.Suspended == true || string.IsNullOrWhiteSpace(Column))
            {
                return;
            }
            if (recordhost?.Metadata?.Attributes?.FirstOrDefault(a => a.LogicalName.Equals(column)) is EnumAttributeMetadata enumattr)
            {
                DataSource = enumattr.OptionSet;
                entityname = recordhost.Metadata.LogicalName;
            }
            else
            {
                DataSource = null;
            }
        }

        public void RecordUpdated()
        {
            if (DesignMode || recordhost == null)
            {
                return;
            }
            populating = true;
            if (recordhost?.Metadata?.LogicalName != entityname)
            {
                GetOptions();
            }
            if (recordhost?[column] is OptionSetValue option)
            {
                SelectByValue(option.Value);
            }
            populating = false;
        }

        public void PopulateOptions()
        {
            if (DesignMode)
            {
                return;
            }
            var selected = SelectedOption;
            var ds = options?.Select(o => new OptionMetadataItem(o, showValue)).ToArray();
            if (sorted && ds?.Length > 0)
            {
                ds = ds.OrderBy(o => o.ToString()).ToArray();
            }
            if (ds != null && addnulloption)
            {
                ds = ds.Prepend(OptionMetadataItem.Empty).ToArray();
            }
            base.DataSource = ds;
            base.Refresh();
            SelectByValue(selected?.Value);
        }

        private void SelectByValue(int? value)
        {
            if (value != null)
            {
                if (base.DataSource is IEnumerable<OptionMetadataItem> ds &&
                    ds?.FirstOrDefault(e => e.Metadata?.Value.Equals(value) == true) is OptionMetadataItem newselected)
                {
                    SelectedItem = newselected;
                }
                else
                {
                    SelectedItem = null;
                    SelectedIndex = -1;
                }
            }
        }
    }
}