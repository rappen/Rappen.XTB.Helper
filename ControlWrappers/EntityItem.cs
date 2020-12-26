namespace Rappen.XTB.Helpers.ControlWrappers
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;

    public class EntityItem : IComboBoxItem
    {
        private EntityMetadata meta = null;

        public bool FriendlyNames { get; set; }

        public EntityItem(EntityMetadata Entity, bool friendlynames)
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
