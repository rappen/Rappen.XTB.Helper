using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public class Filter
    {
        public Entity ParentEntity;
        public Filter ParentFilter;
        public IEnumerable<Filter> Filters;
        public IEnumerable<Condition> Conditions;
        public bool? Or;
        public bool? IsQuickFind;
        public bool? OverrideRecordLimit;

        public Filter(Entity parententity, Filter parentfilter, XmlNode xml)
        {
            ParentEntity = parententity;
            ParentFilter = parentfilter;
            Or = xml.Attribute("type") == "or";
            IsQuickFind = xml.AttributeBool("isquickfindfields");
            OverrideRecordLimit = xml.AttributeBool("overridequickfindrecordlimitenabled");
            Conditions = Condition.List(xml, this);
            Filters = Filter.List(xml, null, this);
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
    }
}
