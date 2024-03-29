﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Attribute : FetchXMLBase
    {
        public Entity Parent;
        public string Name;
        public string Alias;
        public AggregateType? Aggregate;
        public bool? GroupBy;
        public bool? Distinct;
        public bool? UserTimeZone;
        public DateGroupingType? DateGrouping;

        public Attribute(Entity parent, XmlNode xml) : base(parent.Fetch, xml)
        {
            Parent = parent;
            Name = xml.Attribute("name");
            Alias = xml.Attribute("alias");
            if (Enum.TryParse(xml.Attribute("aggregate"), out AggregateType aggregate))
            {
                Aggregate = aggregate;
            }
            GroupBy = xml.AttributeBool("groupby");
            Distinct = xml.AttributeBool("distinct");
            UserTimeZone = xml.AttributeBool("usertimezone");
            if (xml.Attribute("dategrouping") is string group)
            {
                DateGrouping = group.StringToEnum<DateGroupingType>();
            }
        }

        public override string ToString() => ToXML().OuterXml;

        public static List<Attribute> List(XmlNode xml, Entity parent)
        {
            var result = new List<Attribute>(xml.SelectNodes("attribute").OfType<XmlNode>().Select(a => new Attribute(parent, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }

        protected override List<string> GetKnownAttributes() => new List<string> { "name", "alias", "aggregate", "groupby", "distinct", "usertimezone", "dategrouping" };

        protected override List<string> GetKnownNodes() => new List<string>();

        protected override void AddXMLProperties(XmlElement xml)
        {
            xml.AddAttribute("name", Name);
            xml.AddAttribute("alias", Alias);
            if (Fetch.Aggregate == true)
            {
                xml.AddAttribute("aggregate", Aggregate.ToString());
                xml.AddAttribute("groupby", GroupBy);
                xml.AddAttribute("distinct", Distinct);
                xml.AddAttribute("usertimezone", UserTimeZone);
                if (DateGrouping is DateGroupingType group)
                {
                    xml.AddAttribute("dategrouping", group.EnumToString());
                }
            }
        }
    }
}
