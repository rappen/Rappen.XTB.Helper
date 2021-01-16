using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.ControlItems;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMEntityComboBox : ComboBox
    {
        #region Private properties
        private bool showFriendlyNames = true;
        private IEnumerable<EntityMetadata> entities;
        #endregion

        #region Public Constructors

        public XRMEntityComboBox()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

        [Category("Rappen XRM")]
        [Description("Indicates the source of data (collection of EntityMetadata) for the XRMEntityComboBox control.")]
        [Browsable(false)]
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

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("True to show friendly names, False to show logical names and guid etc.")]
        public bool ShowFriendlyNames
        {
            get { return showFriendlyNames; }
            set
            {
                if (value != showFriendlyNames)
                {
                    showFriendlyNames = value;
                    Refresh();
                }
            }
        }

        [Browsable(false)]
        public EntityMetadata SelectedEntity => (SelectedItem is EntityMetadataItem item) ? item.Metadata : null;

        #endregion Public Properties

        #region Public Methods

        public override void Refresh()
        {
            SuspendLayout();
            var selected = SelectedEntity;
            var ds = entities?.Select(e => new EntityMetadataItem(e, showFriendlyNames)).ToArray();
            base.DataSource = ds;
            base.Refresh();
            if (selected != null && ds.FirstOrDefault(e => e.Metadata.LogicalName.Equals(selected.LogicalName)) is EntityMetadataItem newselected)
            {
                SelectedItem = newselected;
            }
            ResumeLayout();
        }

        #endregion Public Methods
    }
}
