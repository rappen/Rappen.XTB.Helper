using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Order : FetchXMLBase
    {
        public Entity Parent;
        public string Name;
        public string Alias;
        public bool? Descending;

        public Order(Entity parent, XmlNode xml) : base(parent.Fetch, xml)
        {
            Parent = parent;
            Name = xml.Attribute("name");
            Alias = xml.Attribute("alias");
            Descending = xml.AttributeBool("descending");
        }

        public override string ToString() => Name;

        public static List<Order> List(XmlNode xml, Entity parent)
        {
            var result = new List<Order>(xml.SelectNodes("order").OfType<XmlNode>().Select(a => new Order(parent, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }

        protected override List<string> GetKnownAttributes() => new List<string> { "name", "alias", "descending" };

        protected override List<string> GetKnownNodes() => new List<string>();

        protected override void AddXMLProperties(XmlElement xml)
        {
            xml.AddAttribute("name", Name);
            xml.AddAttribute("alias", Alias);
            xml.AddAttribute("descending", Descending);
        }
    }
}
