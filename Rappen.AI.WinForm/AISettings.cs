using System.Collections.Generic;
using System.Linq;

namespace Rappen.AI.WinForm
{
    public class AiSettings
    {
        public string Supplier { get; set; }
        public string Model { get; set; }
        public string ApiKey { get; set; }
        public string MyName { get; set; }
    }

    public class AiSupport
    {
        public Prompts Prompts { get; set; } = new Prompts();
        public List<AiSupplier> AiSuppliers { get; set; } = new List<AiSupplier>();

        public AiSupport() { }

        public AiSupplier Supplier(string aisupplier) => AiSuppliers.FirstOrDefault(n => n.Name.Equals(aisupplier));
    }

    public class Prompts
    {
        public string System { get; set; }
        public string CallMe { get; set; }
        public string Update { get; set; }
        public string EntityMeta { get; set; }
        public string AttributeMeta { get; set; }
    }

    public class AiSupplier
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public Prompts Prompts { get; set; } = new Prompts();
        public List<AiModel> Models { get; set; } = new List<AiModel>();

        public AiModel Model(string model) => Models?.FirstOrDefault(n => n.Name.Equals(model));

        public override string ToString() => Name;
    }

    public class AiModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public Prompts Prompts { get; set; } = new Prompts();

        public override string ToString() => Name;
    }
}