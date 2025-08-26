using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.Helpers
{
    public abstract class MetadataForAI
    {
        /// <summary>LogicalName</summary>
        public string L { get; set; }

        /// <summary>DisplayName</summary>
        public string D { get; set; }

        public override string ToString() => $"{L} = {D}";
    }

    public class MetadataForAIEntity : MetadataForAI
    {
        public static List<MetadataForAIEntity> FromEntities(IEnumerable<EntityMetadata> ems)
        {
            var result = new List<MetadataForAIEntity>();
            if (ems == null) return result;
            foreach (var em in ems)
            {
                var aiMeta = FromEntity(em);
                if (aiMeta != null)
                {
                    result.Add(aiMeta);
                }
            }
            return result;
        }

        private static MetadataForAIEntity FromEntity(EntityMetadata em)
        {
            if (em == null ||
                string.IsNullOrEmpty(em.LogicalName) ||
                em.LogicalName.StartsWith("msdyn_") ||
                em.LogicalName.StartsWith("msfp_"))
            {
                return null;
            }
            return new MetadataForAIEntity { L = em.LogicalName, D = em.ToDisplayName() };
        }
    }

    public class MetadataForAIAttribute : MetadataForAI
    {
        /// <summary>Type</summary>
        public string T { get; set; }

        /// <summary>Entity name</summary>
        public object E { get; set; }

        public static List<MetadataForAIAttribute> FromAttributes(IEnumerable<AttributeMetadata> ams, bool IncludeType)
        {
            var result = new List<MetadataForAIAttribute>();
            if (ams == null) return result;
            foreach (var am in ams)
            {
                var aiMeta = FromAttribute(am, IncludeType);
                if (aiMeta != null)
                {
                    result.Add(aiMeta);
                }
            }
            return result;
        }

        private static MetadataForAIAttribute FromAttribute(AttributeMetadata am, bool IncludeType)
        {
            if (am == null ||
                string.IsNullOrEmpty(am.LogicalName) ||
                am.LogicalName.StartsWith("msdyn_") ||
                am.LogicalName.StartsWith("msfp_"))
            {
                return null;
            }
            var result = new MetadataForAIAttribute { L = am.LogicalName, D = am.ToDisplayName() };
            if (IncludeType)
            {
                result.T = am.ToTypeName();
                if (am is LookupAttributeMetadata lookup)
                {
                    result.E = string.Join(",", lookup.Targets);
                }
                else if (am is EnumAttributeMetadata picklist)
                {
                    result.E = MetadataForAIOptionSet.FromChoice(picklist.OptionSet);
                }
                else if (am is MultiSelectPicklistAttributeMetadata multiSelect)
                {
                    result.E = MetadataForAIOptionSet.FromChoice(multiSelect.OptionSet);
                }
            }
            return result;
        }
    }

    public class MetadataForAIOptionSet : MetadataForAI
    {
        /// <summary>OptionSet/Picklist/Choice</summary>
        public List<MetadataForAIOptionsSetValue> O { get; set; }

        public static MetadataForAIOptionSet FromChoice(OptionSetMetadata osm)
        {
            var result = new MetadataForAIOptionSet
            {
                L = osm?.Name,
                D = osm?.DisplayName?.LocalizedLabels?.FirstOrDefault()?.Label,
                O = osm?.Options?
                    .Select(om => MetadataForAIOptionsSetValue.FromOption(om))
                    .Where(o => o != null)
                    .ToList()
            };
            return result;
        }
    }

    public class MetadataForAIOptionsSetValue : MetadataForAI
    {
        /// <summary>Value</summary>
        public int V { get; set; }

        public static MetadataForAIOptionsSetValue FromOption(OptionMetadata om)
        {
            if (om == null || string.IsNullOrEmpty(om.Value.ToString()) || string.IsNullOrEmpty(om.Label?.LocalizedLabels?.FirstOrDefault()?.Label))
            {
                return null;
            }
            return new MetadataForAIOptionsSetValue
            {
                D = om.Label?.LocalizedLabels?.FirstOrDefault()?.Label,
                V = om.Value.Value
            };
        }
    }
}