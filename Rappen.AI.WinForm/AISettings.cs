using System.Collections.Generic;
using System.Linq;

namespace Rappen.AI.WinForm
{
    public class AiSettings
    {
        public string Supplier { get; set; }
        public string Model { get; set; }
        public string ApiKey { get; set; }
    }

    public class AiSupported : List<AiSupplier>
    {
        public AiSupported() { }

        public AiSupplier Supplier(string supplier) => this.FirstOrDefault(n => n.Name.Equals(supplier));
    }

    public class AiSupplier
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public List<AiModel> Models { get; set; } = new List<AiModel>();

        public AiModel Model(string model) => Models?.FirstOrDefault(n => n.Name.Equals(model));
        public override string ToString() => Name;
    }

    public class AiModel
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public override string ToString() => Name;
    }
}