namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;

    public class OptionMetadataItem : IXRMControlItem
    {
        public bool ShowValue { get; set; }

        public OptionMetadata Metadata { get; } = null;

        public static OptionMetadataItem Empty => new OptionMetadataItem();

        private OptionMetadataItem() { }

        public OptionMetadataItem(OptionMetadata option, bool showvalue)
        {
            Metadata = option;
            ShowValue = showvalue;
        }

        public override string ToString()
        {
            if (Metadata == null)
            {
                return string.Empty;
            }
            var result = Metadata.Label?.UserLocalizedLabel?.Label;
            if (string.IsNullOrWhiteSpace(result))
            {
                return Metadata.Value?.ToString();
            }
            return result + (ShowValue ? $" ({Metadata.Value})" : string.Empty);
        }

        public string GetValue()
        {
            return Metadata.Value.ToString();
        }
    }
}