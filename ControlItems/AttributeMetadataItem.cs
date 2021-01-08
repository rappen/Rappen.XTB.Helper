namespace Rappen.XTB.Helpers.ControlItems
{
    using System.Windows.Forms;
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;

    public class AttributeMetadataItem : ICDSControlItem
    {
        public bool FriendlyNames { get; set; }

        public AttributeMetadata Metadata { get; set; } = null;

        public AttributeMetadataItem(AttributeMetadata Attribute, bool friendlynames)
        {
            Metadata = Attribute;
            FriendlyNames = friendlynames;
        }

        public override string ToString() => FriendlyNames ? DisplayName : Metadata.LogicalName;

        public string DisplayName => GetDisplayName();

        private string GetDisplayName()
        {
            var result = Metadata.LogicalName;
            if (Metadata.DisplayName.UserLocalizedLabel != null)
            {
                result = Metadata.DisplayName.UserLocalizedLabel.Label;
            }
            if (result == Metadata.LogicalName && Metadata.DisplayName.LocalizedLabels.Count > 0)
            {
                result = Metadata.DisplayName.LocalizedLabels[0].Label;
            }
            return result;
        }

        public string GetValue()
        {
            return Metadata.LogicalName;
        }

        public static void AddAttributeToComboBox(ComboBox cmb, AttributeMetadata meta, bool allowvirtual, bool friendlynames)
        {
            var add = false;
            if (!friendlynames)
            {
                add = true;
            }
            else
            {
                add = meta.DisplayName != null && meta.DisplayName.LocalizedLabels != null && meta.DisplayName.LocalizedLabels.Count > 0;
                if (meta.AttributeType == AttributeTypeCode.Money && meta.LogicalName.EndsWith("_base"))
                {
                    add = false;
                }
            }
            if (!allowvirtual && meta.AttributeType == AttributeTypeCode.Virtual)
            {
                add = false;
            }
            if (add)
            {
                cmb.Items.Add(new AttributeMetadataItem(meta, friendlynames));
            }
        }
    }
}
