using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Condition : FetchXMLBase
    {
        public Filter Parent;
        public string Attribute;
        public @operator Operator;
        public string Value_;
        public string ValueOf;
        public List<Value> Values;

        public Condition(Filter parent, XmlNode xml) : base(parent.Fetch, xml)
        {
            Parent = parent;
            Attribute = xml.Attribute("attribute");
            if (xml.Attribute("operator") is string oper)
            {
                Operator = oper.ToEnum<@operator>();
            };
            Value_ = xml.Attribute("value");
            ValueOf = xml.Attribute("valueof");
            Values = Value.List(xml, this);
        }

        public static List<Condition> List(XmlNode xml, Filter parent)
        {
            var result = new List<Condition>(xml.SelectNodes("condition").OfType<XmlNode>().Select(a => new Condition(parent, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }

        protected override List<string> GetKnownAttributes() => new List<string> { "attribute", "operator", "value", "valueof" };

        protected override List<string> GetKnownNodes() => new List<string> { "value" };

        private XmlNode ToXMLValue(string value)
        {
            var xml = Fetch.Xml.CreateElement("value");
            xml.Value = value;
            return xml;
        }

        protected override void AddXMLProperties(XmlElement xml)
        {
            xml.AddAttribute("attribute", Attribute);
            if (Operator is @operator oper)
            {
                xml.AddAttribute("operator", oper.EnumToString());
            }
            xml.AddAttribute("value", Value_);
            xml.AddAttribute("valueof", ValueOf);
            Values?.ForEach(a => xml.AppendChild(a.ToXML()));
        }
    }
}