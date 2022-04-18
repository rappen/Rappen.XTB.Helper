using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Order
    {
        public Entity Parent;
        public string Name;
        public string Alias;
        public bool? Descending;

        public Order(Entity parent, XmlNode xml)
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
    }
}
