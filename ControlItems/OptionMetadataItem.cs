namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;

    class OptionMetadataItem : IXRMControlItem
    {
        public OptionMetadata Metadata { get; } = null;

        public OptionMetadataItem(OptionMetadata Option)
        {
            Metadata = Option;
        }

        public override string ToString()
        {
            return Metadata.Label.UserLocalizedLabel.Label + " (" + Metadata.Value.ToString() + ")";
        }

        public string GetValue()
        {
            return Metadata.Value.ToString();
        }
    }
}
