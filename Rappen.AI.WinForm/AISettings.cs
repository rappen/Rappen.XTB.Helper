using System.Collections.Generic;
using System.Linq;

namespace Rappen.AI.WinForm
{
    public class AiSettings
    {
        public string Supplier { get; set; }
        public string Model { get; set; }
        public string ApiKey { get; set; }
        public string CallMe { get; set; }
    }

    public class AiSuppliers : List<AiSupplier>
    {
        public AiSuppliers() { }

        public AiSupplier Supplier(string supplier) => this.FirstOrDefault(n => n.Name.Equals(supplier));
    }

    public class AiSupplier
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string SystemPrompt { get; set; }
        public string UpdatePrompt { get; set; }
        public string CallMe { get; set; }
        public List<AiModel> Models { get; set; } = new List<AiModel>();

        public AiModel Model(string model) => Models?.FirstOrDefault(n => n.Name.Equals(model));

        public string GetCallMe(string callme)
        {
            if (string.IsNullOrWhiteSpace(CallMe) || string.IsNullOrWhiteSpace(callme))
            {
                return string.Empty;
            }
            return CallMe.Replace("{callme}", callme);
        }

        public override string ToString() => Name;
    }

    public class AiModel
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public override string ToString() => Name;
    }
}