using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers.ControlItems;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMRecordTextBox : TextBox
    {
        #region Private properties
        private string displayFormat = string.Empty;
        private bool clickable = false;
        private string logicalName = null;
        private Guid id = Guid.Empty;
        private EntityItem entity;
        private IOrganizationService organizationService;
        private Font font;
        #endregion

        #region Public Constructors

        public XRMRecordTextBox()
        {
            InitializeComponent();
            font = Font;
            base.ReadOnly = true;
            BackColor = SystemColors.Window;
            Click += HandleClick;
        }

        #endregion Public Constructors

        #region Public Properties

        [Category("Rappen XRM")]
        [DisplayName("Record LogicalName")]
        [Description("LogicalName of the entity type to bind")]
        public string LogicalName
        {
            get
            {
                return logicalName;
            }
            set
            {
                if (value?.Equals(logicalName) == true)
                {
                    return;
                }
                logicalName = value;
                id = Guid.Empty;
                entity = null;
                Refresh();
            }
        }

        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        [Category("Rappen XRM")]
        [DisplayName("Record Id")]
        [Description("Id of the record. LogicalName must be set before setting the Id.")]
        public Guid Id
        {
            get
            {
                return id;
            }
            set
            {
                if (value.Equals(id))
                {
                    return;
                }
                id = string.IsNullOrWhiteSpace(logicalName) ? id = Guid.Empty : value;
                LoadRecord();
                Refresh();
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

        [ReadOnly(true)]
        public new bool ReadOnly { get; set; } = true;

        [Browsable(false)]
        public IOrganizationService Service
        {
            get { return organizationService; }
            set
            {
                if (value == organizationService)
                {
                    return;
                }
                Entity = null;
                organizationService = value;
                LoadRecord();
                Refresh();
            }
        }

        [Browsable(false)]
        public EntityReference EntityReference
        {
            get
            {
                var result = entity?.Entity?.ToEntityReference();
                if (result == null)
                {
                    return null;
                }
                result.Name = new EntityItem(entity?.Entity, organizationService).ToString();
                return result;
            }
            set
            {
                if (value?.LogicalName == logicalName && value?.Id.Equals(Id) == true)
                {
                    return;
                }
                LogicalName = value?.LogicalName;
                Id = value?.Id ?? Guid.Empty;
                Refresh();
            }
        }

        [Browsable(false)]
        public Entity Entity
        {
            get
            {
                return entity?.Entity;
            }
            set
            {
                if (entity?.Entity?.Id.Equals(value?.Id) == true)
                {
                    return;
                }
                entity = value != null ? new EntityItem(value, displayFormat, organizationService) : null;
                logicalName = value?.LogicalName;
                id = value?.Id ?? Guid.Empty;
                Refresh();
            }
        }

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
            new XRMRecordEventArgs(entity?.Entity, null).OnRecordEvent(this, RecordClick);
        }

        private void LoadRecord()
        {
            if (organizationService != null && !string.IsNullOrWhiteSpace(logicalName) && !Guid.Empty.Equals(Id))
            {
                var record = organizationService.Retrieve(logicalName, Id, new ColumnSet(true));
                entity = new EntityItem(record, displayFormat, organizationService);
            }
            else
            {
                entity = null;
            }
        }

        #endregion Private Methods

        #region Public Methods

        public override void Refresh()
        {
            if (entity != null && !entity.Format.Equals(displayFormat))
            {
                entity.Format = displayFormat;
            }
            Text = entity?.ToString();
            base.Refresh();
        }

        #endregion Public Methods
    }
}
