using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class LinkEntity : Entity
    {
        public string From;
        public string To;
        public LinkType Type;
        public string Alias;
        public bool? Intersect;
        public bool? Visible;

        public LinkEntity(Entity parententity, Filter parentfilter, XmlNode xml) : base(parententity?.Fetch ?? parentfilter?.Fetch, xml)
        {
            From = xml.Attribute("from");
            To = xml.Attribute("to");
            Enum.TryParse(xml.Attribute("link-type") ?? "inner", out Type);
            Alias = xml.Attribute("alias");
            Intersect = xml.AttributeBool("intersect");
            Visible = xml.AttributeBool("visible");
        }

        public static List<LinkEntity> List(XmlNode xml, Entity parententity, Filter parentfilter)
        {
            var result = new List<LinkEntity>(xml.SelectNodes("link-entity").OfType<XmlNode>().Select(a => new LinkEntity(parententity, parentfilter, a)));
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
            if (Type != LinkType.inner)
            {
                xml.AddAttribute("link-type", Type.ToString());
            }
            ToXMLProperties(xml);
        }
    }
}