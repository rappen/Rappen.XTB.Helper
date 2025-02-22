using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.RappSack
{
    public static class RappSackMeta
    {
        private static Dictionary<IOrganizationService, Dictionary<string, EntityMetadata>> entities = new Dictionary<IOrganizationService, Dictionary<string, EntityMetadata>>();

        public static string[] entityProperties = {
            "LogicalName",
            "DisplayName",
            "DisplayCollectionName",
            "PrimaryIdAttribute",
            "PrimaryNameAttribute",
            "ObjectTypeCode",
            "IsManaged",
            "IsCustomizable",
            "IsCustomEntity",
            "IsIntersect",
            "IsValidForAdvancedFind",
            "DataProviderId",
            "IsAuditEnabled",
            "IsLogicalEntity",
            "IsActivity",
            "IsActivityParty",
            "OwnershipType"
        };
        public static string[] entityDetails = {
            "Attributes",
            "ManyToOneRelationships",
            "OneToManyRelationships",
            "ManyToManyRelationships",
            "SchemaName",
            "LogicalCollectionName",
            "PrimaryIdAttribute"
        };
        public static string[] attributeProperties = {
            "DisplayName",
            "AttributeType",
            "IsValidForRead",
            "AttributeOf",
            "IsManaged",
            "IsCustomizable",
            "IsCustomAttribute",
            "IsValidForAdvancedFind",
            "IsPrimaryId",
            "IsPrimaryName",
            "OptionSet",
            "SchemaName",
            "Targets"
        };

        public static AttributeMetadata GetAttribute(this IOrganizationService service, string entity, string attribute, object value)
        {
            if (value is AliasedValue)
            {
                var aliasedValue = value as AliasedValue;
                entity = aliasedValue.EntityLogicalName;
                attribute = aliasedValue.AttributeLogicalName;
            }
            return GetAttribute(service, entity, attribute);
        }

        public static AttributeMetadata GetAttribute(this IOrganizationService service, string entity, string attribute) =>
            GetEntity(service, entity)?.Attributes?.FirstOrDefault(a => a.LogicalName.Equals(attribute));

        public static EntityMetadata GetEntity(this IOrganizationService service, string entity)
        {
            if (service == null || string.IsNullOrWhiteSpace(entity))
            {
                return null;
            }
            if (!entities.TryGetValue(service, out var serviceEntities))
            {
                serviceEntities = new Dictionary<string, EntityMetadata>();
                try
                {
                    entities.Add(service, serviceEntities);
                }
                catch { }
            }

            if (!serviceEntities.ContainsKey(entity) && LoadEntityDetails(service, entity) is EntityMetadata e)
            {
                try
                {
                    serviceEntities.Add(entity, e);
                }
                catch { }
            }
            if (serviceEntities.TryGetValue(entity, out EntityMetadata meta))
            {
                return meta;
            }
            return null;
        }

        public static AttributeMetadata GetPrimaryAttribute(this IOrganizationService service, string entity) =>
            GetEntity(service, entity)?.Attributes?.FirstOrDefault(a => a.IsPrimaryName == true);

        public static RetrieveMetadataChangesResponse LoadEntities(this IOrganizationService service, int orgMajorVer = 0, int orgMinorVer = 0)
        {
            if (service == null)
            {
                return null;
            }
            var eqe = new EntityQueryExpression();
            eqe.Properties = new MetadataPropertiesExpression(GetEntityDetailsForVersion(entityProperties, orgMajorVer, orgMinorVer));
            var req = new RetrieveMetadataChangesRequest()
            {
                Query = eqe,
                ClientVersionStamp = null
            };
            return service.Execute(req) as RetrieveMetadataChangesResponse;
        }

        public static RetrieveMetadataChangesResponse LoadEntityDetails(this IOrganizationService service, string entityName, int orgMajorVer = 0, int orgMinorVer = 0)
        {
            if (service == null)
            {
                return null;
            }
            var eqe = new EntityQueryExpression();
            eqe.Properties = new MetadataPropertiesExpression(GetEntityDetailsForVersion(entityProperties, orgMajorVer, orgMinorVer));
            string[] details = GetEntityDetailsForVersion(entityDetails, orgMajorVer, orgMinorVer);
            eqe.Properties.PropertyNames.AddRange(details);
            eqe.Criteria.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));
            var aqe = new AttributeQueryExpression();
            aqe.Properties = new MetadataPropertiesExpression(attributeProperties);
            eqe.AttributeQuery = aqe;
            var req = new RetrieveMetadataChangesRequest()
            {
                Query = eqe,
                ClientVersionStamp = null
            };
            return service.Execute(req) as RetrieveMetadataChangesResponse;
        }

        public static EntityMetadata LoadEntityDetails(this IOrganizationService service, string entity)
        {
            var request = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.All,
                LogicalName = entity
            };
            try
            {
                return ((RetrieveEntityResponse)service.Execute(request)).EntityMetadata;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsPOA(this AttributeMetadata attribute) => IsPOA(attribute?.LogicalName);

        public static bool IsPOA(this string attribute) => attribute?.EndsWith("accessrightsmask") == true;

        /// <summary>
        /// Don't try to retrieve properties when this version don't have it.
        /// Got the info from https://github.com/albanian-xrm/PackageHistoryBuilder
        /// </summary>
        /// <param name="entitiesoptions"></param>
        /// <param name="orgMajorVer"></param>
        /// <param name="orgMinorVer"></param>
        /// <returns></returns>
        private static string[] GetEntityDetailsForVersion(string[] entitiesoptions, int orgMajorVer, int orgMinorVer)
        {
            var result = entitiesoptions.ToList();
            if (orgMajorVer < 7 || (orgMajorVer == 7 && orgMinorVer < 1))
            {
                result.Remove("LogicalCollectionName");
            }
            if (orgMajorVer < 8 || (orgMajorVer == 8 && orgMinorVer < 2))
            {
                result.Remove("IsLogicalEntity");
            }
            if (orgMajorVer < 9)
            {
                result.Remove("DataProviderId");
            }
            return result.ToArray();
        }

        public static string ToDisplayName(this EntityMetadata entity, bool includelogicalname = false)
        {
            if (entity == null)
            {
                return string.Empty;
            }
            if (entity.DisplayName?.UserLocalizedLabel?.Label is string label1 && !string.IsNullOrWhiteSpace(label1))
            {
                return label1 + (includelogicalname ? $" ({entity.LogicalName})" : string.Empty);
            }
            if (entity.DisplayName?.LocalizedLabels?.FirstOrDefault()?.Label is string label2 && !string.IsNullOrWhiteSpace(label2))
            {
                return label2 + (includelogicalname ? $" ({entity.LogicalName})" : string.Empty);
            }
            return entity.LogicalName;
        }

        public static string ToCollectionDisplayName(this EntityMetadata entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }
            if (entity.DisplayCollectionName?.UserLocalizedLabel?.Label is string label1 && !string.IsNullOrWhiteSpace(label1))
            {
                return label1;
            }
            if (entity.DisplayCollectionName?.LocalizedLabels?.FirstOrDefault()?.Label is string label2 && !string.IsNullOrWhiteSpace(label2))
            {
                return label2;
            }
            return entity.LogicalCollectionName;
        }

        public static string ToDisplayName(this AttributeMetadata attribute, bool includetype = false)
        {
            if (attribute == null)
            {
                return string.Empty;
            }
            if (attribute.DisplayName?.UserLocalizedLabel?.Label is string label1 && !string.IsNullOrWhiteSpace(label1))
            {
                return label1 + (includetype ? $" ({attribute.ToTypeName()})" : string.Empty);
            }
            if (attribute.DisplayName?.LocalizedLabels?.FirstOrDefault()?.Label is string label2 && !string.IsNullOrWhiteSpace(label2))
            {
                return label2 + (includetype ? $" ({attribute.ToTypeName()})" : string.Empty);
            }
            return attribute.LogicalName;
        }

        public static string ToTypeName(this AttributeMetadata attribute, bool friendlier = false)
        {
            if (attribute?.AttributeType == null)
            {
                return string.Empty;
            }
            var result = attribute.AttributeTypeName?.Value ?? attribute.AttributeType?.ToString();
            if (result?.EndsWith("Type") == true)
            {
                result = result.Substring(0, result.Length - 4);
            }
            if (friendlier)
            {
                result = result
                    .Replace("String", "Text")
                    .Replace("Memo", "Long Text")
                    .Replace("Integer", "Whole Number")
                    .Replace("MultiSelectPicklist", "Choices")
                    .Replace("Picklist", "Choice")
                    .Replace("Boolean", "Yes/No")
                    .Replace("Money", "Currency")
                    .Replace("Uniqueidentifier", "Id");
            }
            return result;
        }
    }
}