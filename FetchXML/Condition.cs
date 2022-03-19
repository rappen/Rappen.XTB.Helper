using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Condition
    {
        public Filter Parent;
        public string Attribute;
        public @operator Operator;
        public string Value;
        public string ValueOf;
        public IEnumerable<string> Values;

        public Condition(Filter parent, XmlNode xml)
        {
            Parent = parent;
            Attribute = xml.Attribute("attribute");
            if (xml.Attribute("operator") is string oper)
            {
                Operator = oper.ToEnum<@operator>();
            };
            Value = xml.Attribute("value");
            ValueOf = xml.Attribute("valueof");
            Values = Condition.ListValues(xml);
        }

        public override string ToString() => Attribute;

        public static List<Condition> List(XmlNode xml, Filter parent)
        {
            var result = new List<Condition>(xml.SelectNodes("condition").OfType<XmlNode>().Select(a => new Condition(parent, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }

        public static List<string> ListValues(XmlNode xml)
        {
            var result = new List<string>(xml.SelectNodes("value").OfType<XmlNode>().Select(a => a.InnerText));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
    }
}
