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

        public LinkEntity(Fetch parentfetch, Entity parententity, XmlNode xml) : base(parentfetch, xml)
        {
            ParentEntity = parententity;
            From = xml.Attribute("from");
            To = xml.Attribute("to");
            Outer = xml.Attribute("link-type") == "outer";
            Alias = xml.Attribute("alias");
            Intersect = xml.AttributeBool("intersect");
            Visible = xml.AttributeBool("visible");
        }

        public override string ToString() => ParentEntity.ToString() + "-" + base.ToString();

        public static List<LinkEntity> List(XmlNode xml, Fetch parentfetch, Entity parententity)
        {
            var result = new List<LinkEntity>(xml.SelectNodes("link-entity").OfType<XmlNode>().Select(a => new LinkEntity(parentfetch, parententity, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
    }
}
