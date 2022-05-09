using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Fetch : FetchXMLBase
    {
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

        internal XmlDocument Xml;

        protected override List<string> GetKnownAttributes() => new List<string> { "top", "count", "distinct", "no-lock", "latematerialize", "returntotalrecordcount", "aggregate", "page", "paging-cookie" };

        protected override List<string> GetKnownNodes() => new List<string> { "entity" };

        public static Fetch FromString(string fetch)
        {
            var doc = new XmlDocument();
            doc.LoadXml(fetch);
            return new Fetch(doc.SelectSingleNode("fetch"));
        }

        public Fetch(XmlNode xml) : base(null, xml)
        {
            Xml = new XmlDocument();
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

        protected override void AddXMLProperties(XmlElement xml)
        {
            xml.AddAttribute("top", Top);
            xml.AddAttribute("count", PageSize);
            xml.AddAttribute("distinct", Distinct);
            xml.AddAttribute("no-lock", NoLock);
            xml.AddAttribute("latematerialize", LateMaterializy);
            xml.AddAttribute("returntotalrecordcount", TotalRecordCount);
            xml.AddAttribute("aggregate", Aggregate);
            xml.AddAttribute("page", PageNumber);
            xml.AddAttribute("paging-cookie", PagingCookie);
            if (Entity != null)
            {
                xml.AppendChild(Entity.ToXML());
            }
        }
    }
}
