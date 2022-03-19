using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Attribute
    {
        public Entity Parent;
        public string Name;
        public string Alias;
        public AggregateType? Aggregate;
        public bool? GroupBy;
        public bool? Distinct;
        public bool? UserTimeZone;
        public DateGroupingType? DateGrouping;

        public Attribute(Entity parent, XmlNode xml)
        {
            Parent = parent;
            Name = xml.Attribute("name");
            Alias = xml.Attribute("alias");
            if (xml.Attribute("aggregate") is string aggr)
            {
                Aggregate = aggr.ToEnum<AggregateType>();
            };
            GroupBy = xml.AttributeBool("groupby");
            Distinct = xml.AttributeBool("distinct");
            UserTimeZone = xml.AttributeBool("usertimezone");
            if (xml.Attribute("dategrouping") is string group)
            {
                DateGrouping = group.ToEnum<DateGroupingType>();
            };
        }

        public override string ToString() => Name;

        public static List<Attribute> List(XmlNode xml, Entity parent)
        {
            var result = new List<Attribute>(xml.SelectNodes("attribute").OfType<XmlNode>().Select(a => new Attribute(parent, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
    }
}
