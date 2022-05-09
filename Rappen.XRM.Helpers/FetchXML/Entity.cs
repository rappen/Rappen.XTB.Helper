using System.Collections.Generic;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Entity : FetchXMLBase
    {
        public string Name;
        public bool AllAttributes;
        public List<Attribute> Attributes;
        public List<Order> Orders;
        public List<Filter> Filters;
        public List<LinkEntity> LinkEntities;

        internal Entity(Fetch parent, XmlNode xml) : base(parent, xml)
        {
            Name = xml.Attribute("name");
            AllAttributes = xml.SelectSingleNode("all-attributes") != null;
            Attributes = Attribute.List(xml, this);
            Orders = Order.List(xml, this);
            Filters = Filter.List(xml, this, null);
            LinkEntities = LinkEntity.List(xml, this);
        }

        protected override void AddXMLProperties(XmlElement xml)
        {
            xml.AddAttribute("name", Name);
            ToXMLProperties(xml);
        }

        protected override List<string> GetKnownAttributes() => new List<string> { "name" };

        protected override List<string> GetKnownNodes() => new List<string> { "attribute", "all-attributes", "order", "filter", "link-entity" };

        protected void ToXMLProperties(XmlElement xml)
        {
            if (AllAttributes)
            {
                xml.AppendChild(Fetch.Xml.CreateElement("all-attributes"));
            }
            Attributes?.ForEach(a => xml.AppendChild(a.ToXML()));
            Orders?.ForEach(o => xml.AppendChild(o.ToXML()));
            Filters?.ForEach(f => xml.AppendChild(f.ToXML()));
            LinkEntities?.ForEach(l => xml.AppendChild(l.ToXML()));
        }
    }
}
