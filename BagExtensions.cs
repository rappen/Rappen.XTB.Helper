using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XTB.Helpers
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
        /// <param name="query"></param>
        /// <returns></returns>
        public static EntityCollection RetrieveMultiple(this IBag bag, QueryBase query)
        {
            bag.Logger.StartSection("RetrieveMultiple");
            var cEntities = bag.Service.RetrieveMultiple(query);
            bag.Logger.Log($"Retrieved {cEntities.Entities.Count} records");
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

        #endregion Public Methods
    }
}