using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.Helpers.Extensions
{
    /// <summary>
    /// Extension methods for IContainable classes
    /// </summary>
    public static class BagExtensions
    {
        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        public static void Create(this IBag bag, Entity entity)
        {
            var id = bag.Service.Create(entity);
            entity.Id = id;
            bag.Logger.Log($"Created {entity.LogicalName} {entity.Id}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        public static void Delete(this IBag bag, Entity entity)
        {
            if (entity.Id.Equals(Guid.Empty))
            {
                bag.Logger.Log("Cannot delete - guid is empty");
                return;
            }
            bag.Service.Delete(entity.LogicalName, entity.Id);
            bag.Logger.Log($"Deleted {entity.LogicalName} {entity.Id}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        /// <param name="columnSet"></param>
        /// <returns></returns>
        public static Entity Retrieve(this IBag bag, string entityName, Guid id, ColumnSet columnSet)
        {
            bag.Logger.StartSection("Retrieve");
            var entity = bag.Service.Retrieve(entityName, id, columnSet);
            bag.Logger.Log($"Retrieved {entityName} {id}");
            bag.Logger.EndSection();
            return entity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static EntityCollection RetrieveMultiple(this IBag bag, QueryBase query)
        {
            var name = query is QueryExpression qex ? qex.EntityName : query.ToString();
            bag.Logger.StartSection($"RetrieveMultiple");
            var cEntities = bag.Service.RetrieveMultiple(query);
            bag.Logger.Log($"Retrieved {cEntities.Entities.Count} {name}");
            bag.Logger.EndSection();
            return cEntities;
        }

        /// <summary>
        /// Save the entity record. If it has a valid Id it will be updated, otherwise new record created.
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        public static void Save(this IBag bag, Entity entity)
        {
            if (entity.Id.Equals(Guid.Empty))
            {
                bag.Create(entity);
            }
            else
            {
                bag.Update(entity);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        public static void Update(this IBag bag, Entity entity)
        {
            bag.Service.Update(entity);
            bag.Logger.Log($"Updated {entity.LogicalName} {entity.Id} with {entity.Attributes.Count} attributes");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="relatedentity"></param>
        /// <param name="intersect"></param>
        public static void Associate(this IBag bag, Entity entity, Entity relatedentity, string intersect)
        {
            var coll = new List<Entity>() { relatedentity };
            Associate(bag, entity, coll, intersect, Int32.MaxValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="relatedentities"></param>
        /// <param name="intersect"></param>
        /// <param name="batchSize"></param>
        public static void Associate(this IBag bag, Entity entity, List<Entity> relatedentities, string intersect, int batchSize)
        {
            var entRefCollection = relatedentities.Select(e => e.ToEntityReference()).ToList();
            Associate(bag, entity, entRefCollection, intersect, batchSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="relatedentities"></param>
        /// <param name="intersect"></param>
        /// <param name="batchSize"></param>
        public static void Associate(this IBag bag, Entity entity, List<EntityReference> relatedentities, string intersect, int batchSize)
        {
            EntityRole? role = null;
            if (relatedentities.Count > 0 && relatedentities[0].LogicalName == entity.LogicalName)
            {   // N:N-relation till samma entitet, då måste man ange en roll, tydligen.
                role = EntityRole.Referencing;
            }

            if (batchSize < 1)
            {
                throw new ArgumentException("Must be a positive number", "batchSize");
            }

            var processed = 0;
            while (processed < relatedentities.Count)
            {
                var batch = new EntityReferenceCollection(relatedentities.Skip(processed).Take(batchSize).ToList());
                processed += batch.Count();

                var req = new AssociateRequest
                {
                    Target = entity.ToEntityReference(),
                    Relationship = new Relationship(intersect)
                    {
                        PrimaryEntityRole = role
                    },
                    RelatedEntities = batch
                };
                bag.Service.Execute(req);
                bag.Logger.Log($"Associated {batch.Count} {(relatedentities.Count > 0 ? relatedentities[0].LogicalName : string.Empty)} with {entity.ToStringExt(bag.Service)}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="relatedentity"></param>
        /// <param name="intersect"></param>
        public static void Disassociate(this IBag bag, Entity entity, Entity relatedentity, string intersect)
        {
            var coll = new List<Entity>() { relatedentity };
            Disassociate(bag, entity, coll, intersect, Int32.MaxValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="relatedentities"></param>
        /// <param name="intersect"></param>
        /// <param name="batchSize"></param>
        public static void Disassociate(this IBag bag, Entity entity, List<Entity> relatedentities, string intersect, int batchSize)
        {
            var entRefCollection = relatedentities.Select(e => e.ToEntityReference()).ToList();
            Disassociate(bag, entity, entRefCollection, intersect, batchSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="relatedentities"></param>
        /// <param name="intersect"></param>
        /// <param name="batchSize"></param>
        public static void Disassociate(this IBag bag, Entity entity, List<EntityReference> relatedentities, string intersect, int batchSize)
        {
            if (batchSize < 1)
            {
                throw new ArgumentException("Must be a positive number", "batchSize");
            }

            var processed = 0;
            while (processed < relatedentities.Count)
            {
                var batch = new EntityReferenceCollection(relatedentities.Skip(processed).Take(batchSize).ToList());
                processed += batch.Count();

                var req = new DisassociateRequest
                {
                    Target = entity.ToEntityReference(),
                    Relationship = new Relationship(intersect),
                    RelatedEntities = batch
                };
                bag.Service.Execute(req);
                bag.Logger.Log($"Disassociated {batch.Count} {(relatedentities.Count > 0 ? relatedentities[0].LogicalName : "")} from {entity.ToStringExt(bag.Service)}");
            }
        }

        public static string GetEnvironmentVariableValue(this IBag container, string variablename, bool throwifnotfound)
        {
            container.Logger.StartSection("GetEnvironmentVariableValue");
            container.Logger.Log($"Variable Name: {variablename}");
            var qe = new QueryExpression(Environmentvariabledefinition.EntityName);
            qe.ColumnSet.AddColumns(Environmentvariabledefinition.PrimaryName, Environmentvariabledefinition.Defaultvalue);
            qe.Criteria.AddCondition(Environmentvariabledefinition.PrimaryName, ConditionOperator.Equal, variablename);
            var ev = qe.AddLink(Environmentvariablevalue.EntityName, Environmentvariablevalue.EnvironmentvariabledefinitionId, Environmentvariabledefinition.PrimaryKey, JoinOperator.LeftOuter);
            ev.EntityAlias = "EV";
            ev.Columns.AddColumns(Environmentvariablevalue.Value);
            var variablevalues = container.RetrieveMultiple(qe);
            if (variablevalues.Entities.Count != 1)
            {
                if (throwifnotfound)
                {
                    throw new InvalidPluginExecutionException($"Found {variablevalues.Entities.Count} environment variables.");
                }
                return string.Empty;
            }
            string result;
            if (variablevalues[0].GetAttributeValue<AliasedValue>("EV.value") is AliasedValue value)
            {
                //
                result = value.Value.ToString();
                container.Logger.Log($"Found Environment Variable value: {result}");
            }
            else
            {
                variablevalues[0].TryGetAttributeValue<string>(Environmentvariabledefinition.Defaultvalue, out result);
                container.Logger.Log($"Found Environment Variable default: {result}");
            }
            container.Logger.EndSection();
            return result;
        }
        #endregion Public Methods
    }
}