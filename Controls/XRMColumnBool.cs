using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMColumnBool : CheckBox, IXRMRecordControl
    {
        private string column = string.Empty;
        private bool showMetaLabels = true;
        private XRMRecordHost recordhost;
        private bool populating;

        public XRMColumnBool()
        {
            InitializeComponent();
            CheckedChanged += XRMRecordCheckBox_CheckedChanged;
        }

        private void XRMRecordCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (DesignMode || populating)
            {
                return;
            }
            if (recordhost == null)
            {
                throw new XRMControlException(this, "RecordHost not set.");
            }
            if (string.IsNullOrWhiteSpace(column))
            {
                MessageBox.Show("Cannot set value, Column property missing.", this.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SetMetaText();
            recordhost[column] = Checked;
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
            }
        }

        [Category("Rappen XRM")]
        [Description("Column to bind the checkbox to.")]
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
                RecordUpdated();
            }
        }

        [Category("Rappen XRM")]
        [Description("Defines if the text of the checkbox shall be shown from the attribute metadata.")]
        [DefaultValue(true)]
        public bool ShowMetadataLabels
        {
            get { return showMetaLabels; }
            set
            {
                if (value == showMetaLabels)
                {
                    return;
                }
                showMetaLabels = value;
                SetMetaText();
            }
        }

        private void SetMetaText()
        {
            if (DesignMode || !showMetaLabels || recordhost == null || recordhost.Metadata == null || string.IsNullOrWhiteSpace(column))
            {
                return;
            }
            if (recordhost?.Metadata.Attributes.FirstOrDefault(a => a.LogicalName == column) is BooleanAttributeMetadata meta)
            {
                Text = (Checked ? meta.OptionSet.TrueOption : meta.OptionSet.FalseOption).Label.UserLocalizedLabel.Label;
            }
        }

        public void RecordUpdated()
        {
            if (DesignMode || recordhost == null || recordhost?.Suspended == true)
            {
                return;
            }
            populating = true;
            if (!string.IsNullOrWhiteSpace(column) && recordhost[column] is bool value)
            {
                Checked = value;
            }
            else
            {
                Checked = false;
            }
            populating = false;
            SetMetaText();
        }
    }
}