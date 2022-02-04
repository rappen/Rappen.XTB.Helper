using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XTB.Helpers.Extensions
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
            "IsActivityParty"
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

        public static AttributeMetadata GetAttribute(this IOrganizationService service, string entity, string attribute)
        {
            var entitymeta = GetEntity(service, entity);
            if (entitymeta != null)
            {
                if (entitymeta.Attributes != null)
                {
                    foreach (var metaattribute in entitymeta.Attributes)
                    {
                        if (metaattribute.LogicalName == attribute)
                        {
                            return metaattribute;
                        }
                    }
                }
            }
            return null;
        }

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

        public static AttributeMetadata GetPrimaryAttribute(this IOrganizationService service, string entity)
        {
            var entitymeta = GetEntity(service, entity);
            if (entitymeta != null)
            {
                if (entitymeta.Attributes != null)
                {
                    foreach (var metaattribute in entitymeta.Attributes)
                    {
                        if (metaattribute.IsPrimaryName == true)
                        {
                            return metaattribute;
                        }
                    }
                }
            }
            return null;
        }

        public static RetrieveMetadataChangesResponse LoadEntities(this IOrganizationService service)
        {
            if (service == null)
            {
                return null;
            }
            var eqe = new EntityQueryExpression();
            eqe.Properties = new MetadataPropertiesExpression(entityProperties);
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
            eqe.Properties = new MetadataPropertiesExpression(entityProperties);
            string[] details = GetEntityDetailsForVersion(orgMajorVer, orgMinorVer);
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
            return ((RetrieveEntityResponse)service.Execute(request)).EntityMetadata;
        }

        private static string[] GetEntityDetailsForVersion(int orgMajorVer, int orgMinorVer)
        {
            var result = entityDetails.ToList();
            if (orgMajorVer < 8)
            {
                result.Remove("LogicalCollectionName");
            }
            if (orgMinorVer < 9)
            {
                result.Remove("DataProviderId");
            }
            return result.ToArray();
        }
    }
}

