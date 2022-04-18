using System;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public class XRMControlException : Exception
    {
        public XRMControlException(Control control, string message) : base(GetControlInfo(control) + message) { }

        private static string GetControlInfo(Control control)
        {
            return control.Name + " (" + control.ToString() + ") error: ";
        }

        public XRMControlException(Control control, string message, Exception innerException) : base(GetControlInfo(control) + message, innerException) { }
    }
}
