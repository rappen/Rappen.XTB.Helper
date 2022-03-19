using System.Collections.Generic;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Entity
    {
        public Fetch Parent;
        public string Name;
        public bool AllAttributes;
        public List<Attribute> Attributes;
        public List<Order> Orders;
        public List<Filter> Filters;
        public List<LinkEntity> LinkEntities;

        public Entity(Fetch parent, XmlNode xml)
        {
            Parent = parent;
            Name = xml.Attribute("name");
            AllAttributes = xml.SelectSingleNode("all-attributes") != null;
            Attributes = Attribute.List(xml, this);
            Orders = Order.List(xml, this);
            Filters = Filter.List(xml, this, null);
            LinkEntities = LinkEntity.List(xml, parent, this);
        }

        public override string ToString() => Name;
    }
}
