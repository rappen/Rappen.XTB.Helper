using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Rappen.XRM.Helpers
{
    public abstract class MetadataForAI
    {
        /// <summary>LogicalName</summary>
        [Description("Logical Name")]
        public string L { get; set; }

        /// <summary>DisplayName</summary>
        [Description("Display Name")]
        public string D { get; set; }

        /// <summary>Description</summary>
        [Description("Description")]
        public string Desc { get; set; }

        protected static bool IgnoreName(string logicalName)
        {
            return string.IsNullOrEmpty(logicalName) ||
                logicalName.StartsWith("msdyn_") ||
                logicalName.StartsWith("msfp_");
        }

        public override string ToString() => $"{L} = {D}";
    }

    public class MetadataForAIEntity : MetadataForAI
    {
        public static List<MetadataForAIEntity> FromEntities(IEnumerable<EntityMetadata> ems)
        {
            var result = new List<MetadataForAIEntity>();
            if (ems == null)
            {
                return result;
            }

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
            if (em == null || IgnoreName(em.LogicalName))
            {
                return null;
            }
            return new MetadataForAIEntity
            {
                L = em.LogicalName,
                D = em.ToDisplayName(),
                Desc = em.ToDescription()
            };
        }
    }

    public class MetadataForAIAttribute : MetadataForAI
    {
        /// <summary>Type</summary>
        [Description("Type")]
        public string T { get; set; }

        /// <summary>Entity name</summary>
        [Description("Entity name")]
        public object E { get; set; }

        /// <summary>Picklist/Choice/OptionSet</summary>
        [Description("Picklist/Choice/Choices/MultiplePicklist/OptionSet")]
        public object P { get; set; }

        public static List<MetadataForAIAttribute> FromAttributes(IEnumerable<AttributeMetadata> ams, bool IncludeType)
        {
            var result = new List<MetadataForAIAttribute>();
            if (ams == null)
            {
                return result;
            }

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
            if (am == null || IgnoreName(am.LogicalName))
            {
                return null;
            }
            var result = new MetadataForAIAttribute
            {
                L = am.LogicalName,
                D = am.ToDisplayName(),
                Desc = am.ToDescription()
            };
            if (IncludeType)
            {
                result.T = am.ToTypeName();
                if (am is LookupAttributeMetadata lookup)
                {
                    result.E = string.Join(",", lookup.Targets);
                }
                else if (am is EnumAttributeMetadata picklist)
                {
                    result.P = MetadataForAIOptionSet.FromChoice(picklist.OptionSet);
                }
                else if (am is MultiSelectPicklistAttributeMetadata multiSelect)
                {
                    result.P = MetadataForAIOptionSet.FromChoice(multiSelect.OptionSet);
                }
            }
            return result;
        }
    }

    public class MetadataForAIRelationship : MetadataForAI
    {
        /// <summary>Relationship kind</summary>
        [Description("Relationship kind: M:1, 1:M or M:M")]
        public string R { get; set; }

        /// <summary>FetchXML from</summary>
        [Description("FetchXML link-entity from attribute")]
        public string F { get; set; }

        /// <summary>FetchXML to</summary>
        [Description("FetchXML link-entity to attribute")]
        public string T { get; set; }

        /// <summary>Intersect entity</summary>
        [Description("Intersect table logical name for many-to-many")]
        public string I { get; set; }

        /// <summary>Intersect from current entity</summary>
        [Description("Intersect column connected to the current table for many-to-many")]
        public string X { get; set; }

        /// <summary>Intersect to related entity</summary>
        [Description("Intersect column connected to the related table for many-to-many")]
        public string Y { get; set; }

        /// <summary>Schema name</summary>
        [Description("Relationship schema name")]
        public string S { get; set; }

        public static List<MetadataForAIRelationship> FromRelationships(EntityMetadata entity, Func<string, EntityMetadata> getEntity = null)
        {
            var result = new List<MetadataForAIRelationship>();
            if (entity == null)
            {
                return result;
            }

            foreach (var rel in entity.ManyToOneRelationships ?? new OneToManyRelationshipMetadata[0])
            {
                var aiMeta = FromManyToOne(rel, getEntity);
                if (aiMeta != null)
                {
                    result.Add(aiMeta);
                }
            }

            foreach (var rel in entity.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
            {
                var aiMeta = FromOneToMany(rel, getEntity);
                if (aiMeta != null)
                {
                    result.Add(aiMeta);
                }
            }

            foreach (var rel in entity.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0])
            {
                var aiMeta = FromManyToMany(entity, rel, getEntity);
                if (aiMeta != null)
                {
                    result.Add(aiMeta);
                }
            }

            return result
                .GroupBy(r => $"{r.R}|{r.L}|{r.F}|{r.T}|{r.I}|{r.X}|{r.Y}|{r.S}")
                .Select(g => g.First())
                .OrderBy(r => r.D ?? r.L)
                .ToList();
        }

        private static MetadataForAIRelationship FromManyToOne(OneToManyRelationshipMetadata rel, Func<string, EntityMetadata> getEntity)
        {
            if (rel == null || IgnoreName(rel.ReferencedEntity))
            {
                return null;
            }

            var related = getEntity?.Invoke(rel.ReferencedEntity);
            return new MetadataForAIRelationship
            {
                L = rel.ReferencedEntity,
                D = related?.ToDisplayName() ?? rel.ReferencedEntity,
                Desc = related?.ToDescription(),
                R = "M:1",
                F = rel.ReferencedAttribute,
                T = rel.ReferencingAttribute,
                S = rel.SchemaName
            };
        }

        private static MetadataForAIRelationship FromOneToMany(OneToManyRelationshipMetadata rel, Func<string, EntityMetadata> getEntity)
        {
            if (rel == null || IgnoreName(rel.ReferencingEntity))
            {
                return null;
            }

            var related = getEntity?.Invoke(rel.ReferencingEntity);
            return new MetadataForAIRelationship
            {
                L = rel.ReferencingEntity,
                D = related?.ToDisplayName() ?? rel.ReferencingEntity,
                Desc = related?.ToDescription(),
                R = "1:M",
                F = rel.ReferencingAttribute,
                T = rel.ReferencedAttribute,
                S = rel.SchemaName
            };
        }

        private static MetadataForAIRelationship FromManyToMany(EntityMetadata current, ManyToManyRelationshipMetadata rel, Func<string, EntityMetadata> getEntity)
        {
            if (current == null || rel == null)
            {
                return null;
            }

            var currentEntity = current.LogicalName;
            var currentPrimaryId = current.PrimaryIdAttribute;
            string relatedEntity;
            string intersectFromCurrent;
            string intersectToRelated;

            if (rel.Entity1LogicalName == currentEntity)
            {
                relatedEntity = rel.Entity2LogicalName;
                intersectFromCurrent = rel.Entity1IntersectAttribute;
                intersectToRelated = rel.Entity2IntersectAttribute;
            }
            else if (rel.Entity2LogicalName == currentEntity)
            {
                relatedEntity = rel.Entity1LogicalName;
                intersectFromCurrent = rel.Entity2IntersectAttribute;
                intersectToRelated = rel.Entity1IntersectAttribute;
            }
            else
            {
                return null;
            }

            if (IgnoreName(relatedEntity) || IgnoreName(rel.IntersectEntityName))
            {
                return null;
            }

            var related = getEntity?.Invoke(relatedEntity);
            return new MetadataForAIRelationship
            {
                L = relatedEntity,
                D = related?.ToDisplayName() ?? relatedEntity,
                Desc = related?.ToDescription(),
                R = "M:M",
                F = currentPrimaryId,
                T = related?.PrimaryIdAttribute,
                I = rel.IntersectEntityName,
                X = intersectFromCurrent,
                Y = intersectToRelated,
                S = rel.SchemaName
            };
        }
    }

    public class MetadataForAIOptionSet : MetadataForAI
    {
        /// <summary>Option Set Values</summary>
        [Description("Option Set Values")]
        public List<MetadataForAIOptionsSetValue> V { get; set; }

        public static MetadataForAIOptionSet FromChoice(OptionSetMetadata osm)
        {
            var result = new MetadataForAIOptionSet
            {
                L = osm?.Name,
                D = osm?.DisplayName?.LocalizedLabels?.FirstOrDefault()?.Label,
                V = osm?.Options?
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
        [Description("The numeric value")]
        public int N { get; set; }

        public static MetadataForAIOptionsSetValue FromOption(OptionMetadata om)
        {
            if (om == null || string.IsNullOrEmpty(om.Value.ToString()) || string.IsNullOrEmpty(om.Label?.LocalizedLabels?.FirstOrDefault()?.Label))
            {
                return null;
            }
            return new MetadataForAIOptionsSetValue
            {
                D = om.Label?.LocalizedLabels?.FirstOrDefault()?.Label,
                N = om.Value.Value
            };
        }
    }
}