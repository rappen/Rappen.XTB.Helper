using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Filter : FetchXMLBase
    {
        public Entity ParentEntity;
        public Filter ParentFilter;
        public List<Filter> Filters;
        public List<Condition> Conditions;
        public List<LinkEntity> LinkEntities;
        public bool? Or;
        public bool? IsQuickFind;
        public bool? OverrideRecordLimit;

        public Filter(Entity parententity, Filter parentfilter, XmlNode xml) : base(parententity?.Fetch ?? parentfilter?.Fetch, xml)
        {
            ParentEntity = parententity;
            ParentFilter = parentfilter;
            Or = xml.Attribute("type") == "or";
            IsQuickFind = xml.AttributeBool("isquickfindfields");
            OverrideRecordLimit = xml.AttributeBool("overridequickfindrecordlimitenabled");
            Conditions = Condition.List(xml, this);
            Filters = Filter.List(xml, null, this);
            LinkEntities = LinkEntity.List(xml, ParentEntity, this);
        }

        public override string ToString() => Or == null ? "filter" : Or == true ? "or" : "and";

        public static List<Filter> List(XmlNode xml, Entity parententity, Filter parentfilter)
        {
            var result = new List<Filter>(xml.SelectNodes("filter").OfType<XmlNode>().Select(a => new Filter(parententity, parentfilter, a)));
            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }

        protected override List<string> GetKnownAttributes() => new List<string> { "type", "isquickfindfields", "overridequickfindrecordlimitenabled" };

        protected override List<string> GetKnownNodes() => new List<string> { "filter", "condition", "link-entity" };

        protected override void AddXMLProperties(XmlElement xml)
        {
            if (Or == true)
            {
                xml.AddAttribute("type", OrToType);
            }
            xml.AddAttribute("isquickfindfields", IsQuickFind);
            xml.AddAttribute("overridequickfindrecordlimitenabled", OverrideRecordLimit);
            Conditions?.ForEach(c => xml.AppendChild(c.ToXML()));
            Filters?.ForEach(f => xml.AppendChild(f.ToXML()));
            LinkEntities?.ForEach(l => xml.AppendChild(l.ToXML()));
        }

        private string OrToType
        {
            get
            {
                if (Or == true)
                {
                    return "or";
                }
                if (Or == false)
                {
                    return "and";
                }
                return null;
            }
        }
    }
}