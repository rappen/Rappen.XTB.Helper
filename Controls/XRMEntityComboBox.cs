using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.ControlItems;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMEntityComboBox : ComboBox
    {
        #region Private properties
        private string displayFormat = string.Empty;
        private IEnumerable<EntityMetadata> entities;
        private IOrganizationService organizationService;
        #endregion

        #region Public Constructors

        public XRMEntityComboBox()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

        [Category("Data")]
        [Description("Indicates the source of data (EntityCollection) for the CDSDataComboBox control.")]
        public new object DataSource
        {
            get
            {
                if (entities != null)
                {
                    return entities;
                }
                return base.DataSource;
            }
            set
            {
                if (value is IEnumerable<EntityMetadata> newentities)
                {
                    entities = newentities;
                    Refresh();
                }
            }
        }

        [Category("Data")]
        [DisplayName("Display Format")]
        [Description("Single attribute from datasource to display for items, or use {{attributename}} syntax freely.")]
        public string DisplayFormat
        {
            get { return displayFormat; }
            set
            {
                if (value != displayFormat)
                {
                    displayFormat = value;
                    Refresh();
                }
            }
        }

        [Browsable(false)]
        public IOrganizationService OrganizationService
        {
            get { return organizationService; }
            set
            {
                organizationService = value;
                Refresh();
            }
        }

        [Browsable(false)]
        public EntityMetadataItem SelectedEntity => (SelectedItem is EntityMetadataItem item) ? item : null;

        #endregion Public Properties

        #region Public Methods

        public override void Refresh()
        {
            SuspendLayout();
            var selected = SelectedEntity;
            //var ds = entities?.Select(e => new EntityWrapper(e, displayFormat, organizationService)).ToArray();
            //base.DataSource = ds;
            //base.Refresh();
            //if (selected != null && ds.FirstOrDefault(e => e.Entity.Id.Equals(selected.Id)) is EntityWrapper newselected)
            //{
            //    SelectedItem = newselected;
            //}
            ResumeLayout();
        }

        #endregion Public Methods
    }
}
