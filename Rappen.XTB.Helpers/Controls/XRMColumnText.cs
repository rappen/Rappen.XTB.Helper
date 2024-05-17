using Rappen.XTB.Helpers.Interfaces;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMColumnText : TextBox, IXRMRecordControl
    {
        #region Private properties

        private XRMRecordHost recordhost;
        private string column;
        private string displayFormat = string.Empty;
        private bool populating = false;

        #endregion Private properties

        #region Public Constructors

        public XRMColumnText()
        {
            InitializeComponent();
            TextChanged += XRMRecordTextBox_TextChanged;
        }

        private void XRMRecordTextBox_TextChanged(object sender, EventArgs e)
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
                MessageBoxEx.Show(this, "Cannot set value, Column property missing.", this.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            recordhost[column] = Text;
        }

        #endregion Public Constructors

        #region Public Properties

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
        [Description("Column to bind the textbox to. Required for edit mode.")]
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
                if (!string.IsNullOrWhiteSpace(column))
                {
                    displayFormat = string.Empty;
                }
                RecordUpdated();
            }
        }

        [Category("Rappen XRM")]
        [Description("XRM Token to display column value. See https://jonasr.app/xrm-tokens/")]
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
                if (!string.IsNullOrWhiteSpace(displayFormat))
                {
                    column = string.Empty;
                    ReadOnly = true;
                }
                RecordUpdated();
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void RecordUpdated()
        {
            if (recordhost?.Suspended == true)
            {
                return;
            }
            populating = true;
            if (!string.IsNullOrWhiteSpace(column))
            {
                var value = recordhost?[column];
                if (value == null)
                {
                    Text = string.Empty;
                }
                else if (value is string stringvalue)
                {
                    Text = stringvalue;
                }
                else
                {
                    Text = recordhost?.EntityItem?.GetFormattedText(column);
                }
            }
            else
            {
                Text = recordhost?.EntityItem?.GetFormattedText(displayFormat);
            }
            populating = false;
        }

        public void SetValueToRecord()
        {
            if (DesignMode || populating)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(column))
            {
                MessageBoxEx.Show(this, "Cannot set value, Column property missing.", this.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            recordhost[column] = Text;
        }

        #endregion Public Methods
    }
}