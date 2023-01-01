using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class MetadataExtensions
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
                entities.Add(service, serviceEntities);
            }

            if (!serviceEntities.ContainsKey(entity) && LoadEntityDetails(service, entity) is EntityMetadata e)
            {
                serviceEntities.Add(entity, e);
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
    }
}