namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XRM.Helpers.Extensions;
    using Rappen.XTB.Helpers.Interfaces;
    using System.Windows.Forms;

    public class AttributeMetadataItem : IXRMControlItem
    {
        public bool FriendlyNames { get; set; }

        public bool ShowTypes { get; set; }

        public AttributeMetadata Metadata { get; } = null;

        public AttributeMetadataItem(AttributeMetadata Attribute, bool friendlynames, bool showtypes)
        {
            Metadata = Attribute;
            FriendlyNames = friendlynames;
            ShowTypes = showtypes;
        }

        public AttributeMetadataItem(IOrganizationService service, string entity, string attribute, bool friendlynames, bool showtypes)
            : this(service.GetAttribute(entity, attribute), friendlynames, showtypes) { }

        public override string ToString() => (FriendlyNames ? Metadata?.ToDisplayName(ShowTypes) : Metadata?.LogicalName) ?? string.Empty;

        public string GetValue()
        {
            return Metadata.LogicalName;
        }

        public static void AddAttributeToComboBox(ComboBox cmb, AttributeMetadata meta, bool allowvirtual, bool friendlynames, bool includetypeindisplayname)
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
            if (!allowvirtual && meta.AttributeType == AttributeTypeCode.Virtual && !(meta is MultiSelectPicklistAttributeMetadata))
            {
                add = false;
            }
            if (add)
            {
                cmb.Items.Add(new AttributeMetadataItem(meta, friendlynames, includetypeindisplayname));
            }
        }
    }
}