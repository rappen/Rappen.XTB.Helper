using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Value : FetchXMLBase
    {
        private string value;

        internal Value(Condition parent, XmlElement xml) : base(parent.Fetch, xml)
        {
            value = xml.InnerText;
        }

        protected override void AddXMLProperties(XmlElement xml)
        {
            xml.InnerText = value;
        }

        protected override List<string> GetKnownAttributes() => new List<string>();

        protected override List<string> GetKnownNodes() => new List<string>();

        public static List<Value> List(XmlNode xml, Condition parent)
        {
            var result = new List<Value>(xml.SelectNodes("value").OfType<XmlElement>().Select(a => new Value(parent, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
    }
}