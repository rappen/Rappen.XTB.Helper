using Rappen.XTB.Helpers.Interfaces;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMColumnDateTime : DateTimePicker, IXRMRecordControl
    {
        private string column = string.Empty;
        private XRMRecordHost recordhost;
        private bool populating;

        public XRMColumnDateTime()
        {
            InitializeComponent();
            ValueChanged += XRMColumnDateTime_ValueChanged;
        }

        private void XRMColumnDateTime_ValueChanged(object sender, EventArgs e)
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
        [Description("Column to bind the data time picker to.")]
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
            if (recordhost != null && !string.IsNullOrWhiteSpace(column) && recordhost[column] is DateTime value)
            {
                Value = value;
            }
            else
            {
                Value = DateTime.Now;
            }
            populating = false;
        }
    }
}