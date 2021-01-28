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
        private string displayFormat = string.Empty;
        private bool clickable = false;
        private Font font;
        private XRMRecordHost recordhost;
        private bool populating = false;
        private string column;
        #endregion

        #region Public Constructors

        public XRMColumnText()
        {
            InitializeComponent();
            font = Font;
            Click += HandleClick;
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
                MessageBox.Show("Cannot set value, Column property missing.", this.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        [Description("Column to bind the textbox to.")]
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
                PopulateFromRecord();
            }
        }

        [Category("Rappen XRM")]
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
                if (!string.IsNullOrWhiteSpace(displayFormat))
                {
                    column = string.Empty;
                    ReadOnly = true;
                }
                PopulateFromRecord();
            }
        }

        [DefaultValue(false)]
        [Category("Rappen XRM")]
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

        //[ReadOnly(true)]
        //public new bool ReadOnly { get; set; } = true;

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
            new XRMRecordEventArgs(RecordHost?.Record, null).OnRecordEvent(this, RecordClick);
        }

        #endregion Private Methods

        #region Public Methods

        public void PopulateFromRecord()
        {
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
                MessageBox.Show("Cannot set value, Column property missing.", this.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            recordhost[column] = Text;
        }

        #endregion Public Methods
    }
}
