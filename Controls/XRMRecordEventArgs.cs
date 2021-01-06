using Microsoft.Xrm.Sdk;
using System;

namespace Rappen.XTB.Helpers.Controls
{
    public class XRMRecordEventArgs : EventArgs
    {
        public XRMRecordEventArgs(Entity entity, string attribute)
        {
            Entity = entity;
            Attribute = attribute;
        }

        public Entity Entity { get; }

        public string Attribute { get; }

        public object Value { get { return Entity != null && Entity.Contains(Attribute) ? Entity[Attribute] : null; } }

        public void OnRecordEvent(object sender, XRMRecordEventHandler RecordEventHandler)
        {
            RecordEventHandler?.Invoke(sender, this);
        }
    }

    public delegate void XRMRecordEventHandler(object sender, XRMRecordEventArgs e);
}
