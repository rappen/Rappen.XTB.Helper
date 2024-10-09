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
            base.Text = Text;
            CheckStateChanged += TriCheckBox_CheckStateChanged;
        }

        private void TriCheckBox_CheckStateChanged(object sender, System.EventArgs e)
        {
            base.Text = Text;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public new bool ThreeState => true;

        [ReadOnly(true)]
        [Browsable(false)]
        public new string Text
        {
            get
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
            set { }
        }

        [Category("Rappen")]
        [DefaultValue("False")]
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
        [DefaultValue("True")]
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
        [DefaultValue("Indeterminate")]
        public string TextIndeterminate
        {
            get => textIndeterminate;
            set
            {
                textIndeterminate = value;
                TriCheckBox_CheckStateChanged(null, null);
            }
        }
    }
}