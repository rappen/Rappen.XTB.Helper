namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;

    public class EntityMetadataItem : ICDSControlItem
    {
        private EntityMetadata meta = null;

        public bool FriendlyNames { get; set; }

        public EntityMetadataItem(EntityMetadata Entity, bool friendlynames)
        {
            meta = Entity;
            FriendlyNames = friendlynames;
        }

        public override string ToString()
        {
            var result = meta.LogicalName;
            if (FriendlyNames)
            {
                if (meta.DisplayName.UserLocalizedLabel != null)
                {
                    result = meta.DisplayName.UserLocalizedLabel.Label;
                }
                if (result == meta.LogicalName && meta.DisplayName.LocalizedLabels.Count > 0)
                {
                    result = meta.DisplayName.LocalizedLabels[0].Label;
                }
            }
            return result;
        }

        public string GetValue()
        {
            return meta.LogicalName;
        }
    }
}
