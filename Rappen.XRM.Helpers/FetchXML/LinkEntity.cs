using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class LinkEntity : Entity
    {
        public Entity ParentEntity;
        public string From;
        public string To;
        public bool? Outer;
        public string Alias;
        public bool? Intersect;
        public bool? Visible;

        public LinkEntity(Entity parent, XmlNode xml) : base(parent.Fetch, xml)
        {
            ParentEntity = parent;
            From = xml.Attribute("from");
            To = xml.Attribute("to");
            Outer = xml.Attribute("link-type") == "outer";
            Alias = xml.Attribute("alias");
            Intersect = xml.AttributeBool("intersect");
            Visible = xml.AttributeBool("visible");
        }

        public static List<LinkEntity> List(XmlNode xml, Entity parententity)
        {
            var result = new List<LinkEntity>(xml.SelectNodes("link-entity").OfType<XmlNode>().Select(a => new LinkEntity(parententity, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }

        public override string ToString() => ToXML().OuterXml;

        protected override List<string> GetKnownAttributes() => new List<string> { "name", "from", "to", "link-type", "alias", "intersect", "visible" };

        protected override List<string> GetKnownNodes() => new List<string> { "attribute", "all-attributes", "order", "filter", "link-entity" };

        protected override void AddXMLProperties(XmlElement xml)
        {
            xml.AddAttribute("name", Name);
            xml.AddAttribute("from", From);
            xml.AddAttribute("to", To);
            xml.AddAttribute("alias", Alias);
            xml.AddAttribute("intersect", Intersect);
            xml.AddAttribute("visible", Visible);
            if (Outer is bool outer)
            {
                xml.AddAttribute("link-type", outer ? "outer" : "inner");
            }
            ToXMLProperties(xml);
        }
    }
}