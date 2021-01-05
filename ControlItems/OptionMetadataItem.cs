namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;

    class OptionMetadataItem : ICDSControlItem
    {
        public OptionMetadata meta = null;

        public OptionMetadataItem(OptionMetadata Option)
        {
            meta = Option;
        }

        public override string ToString()
        {
            return meta.Label.UserLocalizedLabel.Label + " (" + meta.Value.ToString() + ")";
        }

        public string GetValue()
        {
            return meta.Value.ToString();
        }
    }
}
