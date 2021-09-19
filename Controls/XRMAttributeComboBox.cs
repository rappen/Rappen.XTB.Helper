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
        private bool sorted = true;
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
                    attributes = null;
                }
                Refresh();
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

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("Defines if the attributes should be sorted alphabetically, based on selected layout")]
        public new bool Sorted
        {
            get { return sorted; }
            set
            {
                base.Sorted = false;
                if (value != sorted)
                {
                    sorted = value;
                    Refresh();
                }
            }
        }

        [Browsable(false)]
        public AttributeMetadata SelectedAttribute => (SelectedItem is AttributeMetadataItem item) ? item.Metadata : null;

        #endregion Public Properties

        #region Public Methods

        public override void Refresh()
        {
            SuspendLayout();
            var selected = SelectedAttribute;
            var ds = attributes?.Select(a => new AttributeMetadataItem(a, showFriendlyNames)).ToArray();
            if (sorted && ds?.Length > 0)
            {
                ds = ds.OrderBy(a => a.ToString()).ToArray();
            }
            base.DataSource = ds;
            base.Refresh();
            if (selected != null && ds?.FirstOrDefault(a => a.Metadata.LogicalName.Equals(selected.LogicalName)) is AttributeMetadataItem newselected)
            {
                SelectedItem = newselected;
            }
            ResumeLayout();
        }
        
        public void SetSelected(string attributelogicalname)
        {
            if (!string.IsNullOrEmpty(attributelogicalname) &&
                base.DataSource is IEnumerable<AttributeMetadataItem> ds &&
                ds?.FirstOrDefault(e => e.Metadata.LogicalName.Equals(attributelogicalname)) is AttributeMetadataItem newselected)
            {
                SelectedItem = newselected;
            }
            else
            {
                SelectedItem = null;
                SelectedIndex = -1;
            }
        }

        public void SetSelectedPrimaryId()
        {
            if (base.DataSource is IEnumerable<AttributeMetadataItem> ds &&
                ds?.FirstOrDefault(e => e.Metadata.IsPrimaryId == true) is AttributeMetadataItem newselected)
            {
                SelectedItem = newselected;
            }
            else
            {
                SelectedItem = null;
                SelectedIndex = -1;
            }
        }

        public void SetSelectedPrimaryName()
        {
            if (base.DataSource is IEnumerable<AttributeMetadataItem> ds &&
                ds?.FirstOrDefault(e => e.Metadata.IsPrimaryName == true) is AttributeMetadataItem newselected)
            {
                SelectedItem = newselected;
            }
            else
            {
                SelectedItem = null;
                SelectedIndex = -1;
            }
        }

        #endregion Public Methods
    }
}
