using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Fetch
    {
        internal XmlDocument Xml;

        public int? Top;
        public int? PageSize;
        public bool? Distinct;
        public bool? NoLock;
        public bool? LateMaterializy;
        public bool? TotalRecordCount;
        public bool? Aggregate;
        public int? PageNumber;
        public string PagingCookie;
        public Entity Entity;

        public Fetch(string fetch)
        {
            Xml = new XmlDocument();
            Xml.LoadXml(fetch);
            FromXml(Xml.SelectSingleNode("fetch"));
        }

        public Fetch(XmlNode fetch)
        {
            Xml = new XmlDocument();
            FromXml(fetch);
        }

        private void FromXml(XmlNode xml)
        {
            if (xml is XmlDocument)
            {
                xml = xml.SelectSingleNode("fetch");
            }
            Top = xml.AttributeInt("top");
            PageSize = xml.AttributeInt("count");
            Distinct = xml.AttributeBool("distinct");
            NoLock = xml.AttributeBool("no-lock");
            LateMaterializy = xml.AttributeBool("latematerialize");
            TotalRecordCount = xml.AttributeBool("returntotalrecordcount");
            Aggregate = xml.AttributeBool("aggregate");
            PageNumber = xml.AttributeInt("page");
            PagingCookie = xml.Attribute("paging-cookie");
            if (xml?.SelectSingleNode("entity") is XmlNode entity)
            {
                Entity = new Entity(this, entity);
            }
        }

        public override string ToString() => ToXML().OuterXml;

        private XmlDocument ToXML()
        {
            Xml = new XmlDocument();
            var fetch = Xml.CreateElement("fetch");
            fetch.AddAttribute("top", Top);
            fetch.AddAttribute("count", PageSize);
            fetch.AddAttribute("distinct", Distinct);
            fetch.AddAttribute("no-lock", NoLock);
            fetch.AddAttribute("latematerialize", LateMaterializy);
            fetch.AddAttribute("returntotalrecordcount", TotalRecordCount);
            fetch.AddAttribute("aggregate", Aggregate);
            fetch.AddAttribute("page", PageNumber);
            fetch.AddAttribute("paging-cookie", PagingCookie);
            Xml.AppendChild(fetch);
            if (Entity != null)
            {
                fetch.AppendChild(Entity.ToXML());
            }
            return Xml;
        }
    }
}
