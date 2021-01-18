using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.ControlItems;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMOptionSetComboBox : ComboBox
    {
        #region Private properties
        private bool showValue = true;
        private bool sorted = true;
        private IEnumerable<OptionMetadata> options;
        #endregion

        #region Public Constructors

        public XRMOptionSetComboBox()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

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
                Refresh();
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
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("Defines if the entities should be sorted alphabetically, based on selected layout")]
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
        public OptionMetadata SelectedOption => (SelectedItem is OptionMetadataItem item) ? item.Metadata : null;

        #endregion Public Properties

        #region Public Methods

        public override void Refresh()
        {
            SuspendLayout();
            var selected = SelectedOption;
            var ds = options?.Select(o => new OptionMetadataItem(o, showValue)).ToArray();
            if (sorted && ds?.Length > 0)
            {
                ds = ds.OrderBy(o => o.ToString()).ToArray();
            }
            base.DataSource = ds;
            base.Refresh();
            if (selected != null && ds?.FirstOrDefault(e => e.Metadata.Value.Equals(selected.Value)) is OptionMetadataItem newselected)
            {
                SelectedItem = newselected;
            }
            ResumeLayout();
        }

        #endregion Public Methods
    }
}
