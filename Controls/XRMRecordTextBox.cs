using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers.ControlItems;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMRecordTextBox : TextBox, IXRMRecordControl
    {
        #region Private properties
        private string displayFormat = string.Empty;
        private bool clickable = false;
        private XRMRecordControlBase controlBase;
        private Font font;
        #endregion

        #region Public Constructors

        public XRMRecordTextBox()
        {
            InitializeComponent();
            controlBase = new XRMRecordControlBase(this);
            font = Font;
            base.ReadOnly = true;
            BackColor = SystemColors.Window;
            Click += HandleClick;
        }

        #endregion Public Constructors

        #region Public Properties

        [Category("Rappen XRM")]
        [DisplayName("Display Format")]
        [Description("Single attribute from datasource to display for items, or use XRM Tokens syntax freely https://jonasr.app/xrm-tokens/")]
        public string DisplayFormat
        {
            get { return displayFormat; }
            set
            {
                if (value?.Equals(displayFormat) == true)
                {
                    return;
                }
                displayFormat = value;
                Refresh();
            }
        }

        [DefaultValue(false)]
        [Category("Rappen XRM")]
        [DisplayName("Record Clickable")]
        [Description("Displays the record text as a clickable text and fires RecordClick event when clicked")]
        public bool Clickable
        {
            get
            {
                return clickable;
            }
            set
            {
                if (clickable.Equals(value))
                {
                    return;
                }
                clickable = value;
                if (clickable)
                {
                    ForeColor = SystemColors.HotTrack;
                    Font = new Font(font, Font.Style | FontStyle.Underline);
                    Cursor = Cursors.Hand;
                }
                else
                {
                    ForeColor = SystemColors.ControlText;
                    Font = font;
                    Cursor = Cursors.IBeam;
                }
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

        [ReadOnly(true)]
        public new bool ReadOnly { get; set; } = true;

        #endregion Public Properties

        #region Published events

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordClick;

        #endregion Published Events

        #region Private Methods

        private void HandleClick(object sender, EventArgs e)
        {
            if (!clickable)
            {
                return;
            }
            new XRMRecordEventArgs(Entity, null).OnRecordEvent(this, RecordClick);
        }

        #endregion Private Methods

        #region Public Methods

        public override void Refresh()
        {
            if (controlBase.EntityItem != null && !controlBase.EntityItem.Format.Equals(displayFormat))
            {
                controlBase.EntityItem.Format = displayFormat;
            }
            Text = controlBase.EntityItem?.ToString();
            base.Refresh();
        }

        #endregion Public Methods
    }
}
