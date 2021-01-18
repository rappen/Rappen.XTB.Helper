using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.ControlItems;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMAttributeComboBox : ComboBox
    {
        #region Private properties
        private bool showFriendlyNames = true;
        private IEnumerable<AttributeMetadata> attributes;
        #endregion

        #region Public Constructors

        public XRMAttributeComboBox()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

        [Category("Rappen XRM")]
        [Description("Indicates the source of data (EntityMetadata or collection of AttributeMetadata) for the XRMAttributeComboBox control.")]
        [Browsable(false)]
        public new object DataSource
        {
            get
            {
                if (attributes != null)
                {
                    return attributes;
                }
                return base.DataSource;
            }
            set
            {
                var validmeta = true;
                if (value is EntityMetadata entity)
                {
                    attributes = entity.Attributes;
                }
                else if (value is IEnumerable<AttributeMetadata> newattributes)
                {
                    attributes = newattributes;
                }
                else
                {
                    validmeta = false;
                }
                if (validmeta)
                {
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

        // Sorted not supported for databound combobox
        [Browsable(false)]
        public new bool Sorted { get; } = false;

        [Browsable(false)]
        public AttributeMetadata SelectedAttribute => (SelectedItem is AttributeMetadataItem item) ? item.Metadata : null;

        #endregion Public Properties

        #region Public Methods

        public override void Refresh()
        {
            SuspendLayout();
            var selected = SelectedAttribute;
            var ds = attributes?.Select(e => new AttributeMetadataItem(e, showFriendlyNames)).ToArray();
            base.DataSource = ds;
            base.Refresh();
            if (selected != null && ds.FirstOrDefault(a => a.Metadata.LogicalName.Equals(selected.LogicalName)) is AttributeMetadataItem newselected)
            {
                SelectedItem = newselected;
            }
            ResumeLayout();
        }

        #endregion Public Methods
    }
}
