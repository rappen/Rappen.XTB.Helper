namespace Rappen.XTB.Helpers.ControlWrappers
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;

    class OptionsetItem : IComboBoxItem
    {
        public OptionMetadata meta = null;

        public OptionsetItem(OptionMetadata Option)
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
