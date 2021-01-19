using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.Extensions;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMRecordCheckBox : CheckBox, IXRMRecordControl
    {
        private XRMRecordControlBase controlBase;
        private string attribute = string.Empty;
        private bool showMetaLabels = true;

        public XRMRecordCheckBox()
        {
            InitializeComponent();
            controlBase = new XRMRecordControlBase(this);
            CheckedChanged += XRMRecordCheckBox_CheckedChanged;
        }

        private void XRMRecordCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (showMetaLabels)
            {
                SetMetaText();
            }
        }

        [Category("Rappen XRM")]
        [Description("Attribute to bind the checkbox to.")]
        public string Attribute
        {
            get { return attribute; }
            set
            {
                if (value?.Equals(attribute) == true)
                {
                    return;
                }
                attribute = value;
                Refresh();
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
                Refresh();
            }
        }

        [Category("Rappen XRM")]
        [DisplayName("Record LogicalName")]
        [Description("LogicalName of the entity type to bind")]
        public string LogicalName
        {
            get => controlBase.GetLogicalName();
            set => controlBase.SetLogicalName(value);
        }

        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        [Category("Rappen XRM")]
        [DisplayName("Record Id")]
        [Description("Id of the record. LogicalName must be set before setting the Id.")]
        public Guid Id
        {
            get => controlBase.GetId();
            set => controlBase.SetId(value);
        }

        [Browsable(false)]
        public IOrganizationService Service
        {
            get => controlBase.GetService();
            set => controlBase.SetService(value);
        }

        [Browsable(false)]
        public EntityReference EntityReference
        {
            get => controlBase.GetEntityReference();
            set => controlBase.SetEntityReference(value);
        }

        [Browsable(false)]
        public Entity Entity
        {
            get => controlBase.GetEntity();
            set => controlBase.SetEntity(value);
        }

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordClick;

        public void Refresh()
        {
            Checked = controlBase.EntityItem?.Entity?.Property(attribute, false) == true;
            if (showMetaLabels)
            {
                SetMetaText();
            }
            base.Refresh();
        }

        private void SetMetaText()
        {
            if (controlBase.EntityItem?.Metadata.Metadata.Attributes.FirstOrDefault(a => a.LogicalName == attribute) is BooleanAttributeMetadata meta)
            {
                Text = (Checked ? meta.OptionSet.TrueOption : meta.OptionSet.FalseOption).Label.UserLocalizedLabel.Label;
            }
        }

    }
}