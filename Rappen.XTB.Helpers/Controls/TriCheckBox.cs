using Microsoft.Xrm.Sdk.Workflow;
using System.ComponentModel;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class TriCheckBox : CheckBox
    {
        private string textUnchecked = "False";
        private string textChecked = "True";
        private string textIndeterminate = "Indeterminate";

        public TriCheckBox()
        {
            InitializeComponent();
            base.ThreeState = true;
            base.Text = GetText();
            CheckStateChanged += TriCheckBox_CheckStateChanged;
        }

        private void TriCheckBox_CheckStateChanged(object sender, System.EventArgs e)
        {
            base.Text = GetText();
        }

        [Category("Rappen")]
        [ReadOnly(true)]
        public new bool ThreeState => true;

        [Category("Rappen")]
        [ReadOnly(true)]
        public new string Text { get { return GetText(); } set { } }

        [Category("Rappen")]
        [Default("False")]
        public string TextUnchecked
        {
            get => textUnchecked;
            set
            {
                textUnchecked = value;
                TriCheckBox_CheckStateChanged(null, null);
            }
        }

        [Category("Rappen")]
        [Default("True")]
        public string TextChecked
        {
            get => textChecked;
            set
            {
                textChecked = value;
                TriCheckBox_CheckStateChanged(null, null);
            }
        }

        [Category("Rappen")]
        [Default("Indeterminate")]
        public string TextIndeterminate
        {
            get => textIndeterminate;
            set
            {
                textIndeterminate = value;
                TriCheckBox_CheckStateChanged(null, null);
            }
        }

        private string GetText()
        {
            switch (CheckState)
            {
                case CheckState.Unchecked:
                    return TextUnchecked;

                case CheckState.Checked:
                    return TextChecked;

                default:
                    return TextIndeterminate;
            }
        }
    }
}