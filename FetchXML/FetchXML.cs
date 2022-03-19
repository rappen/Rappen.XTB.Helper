using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class FetchXML
    {
        public Fetch Fetch;

        public FetchXML(string fetchxml)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(fetchxml);
            Fetch = new Fetch(xml.SelectSingleNode("fetch"));
        }

        public FetchXML(XmlDocument xml)
        {
            Fetch = new Fetch(xml.SelectSingleNode("fetch"));
        }
    }
}
