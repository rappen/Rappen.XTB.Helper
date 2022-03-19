using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Fetch
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

        public Fetch(XmlNode xml)
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
    }
}
