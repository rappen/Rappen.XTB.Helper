using Rappen.XTB.Helpers.Interfaces;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMColumnNumber : NumericUpDown, IXRMRecordControl
    {
        private string column = string.Empty;
        private XRMRecordHost recordhost;
        private bool populating;

        public XRMColumnNumber()
        {
            InitializeComponent();
            ValueChanged += XRMColumnNumber_ValueChanged;
        }

        private void XRMColumnNumber_ValueChanged(object sender, EventArgs e)
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
            recordhost[column] = Value;
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
        [Description("Column to bind the numeric up/down to.")]
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

        public void RecordUpdated()
        {
            if (DesignMode || recordhost?.Suspended == true)
            {
                return;
            }
            populating = true;
            if (recordhost != null && !string.IsNullOrWhiteSpace(column))
            {
                var col = recordhost[column];
                if (col is long longvalue)
                {
                    Value = longvalue;
                }
                else if (col is decimal decvalue)
                {
                    Value = decvalue;
                }
                else if (col is int intvalue)
                {
                    Value = intvalue;
                }
                else
                {
                    Value = 0;
                }
            }
            else
            {
                Value = 0;
            }
            populating = false;
        }
    }
}