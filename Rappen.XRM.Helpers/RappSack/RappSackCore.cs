using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Rappen.XRM.Helpers.RappSack
{
    public abstract class RappSackCore : IOrganizationService
    {
        private IOrganizationService service;
        private RappSackTracerCore tracer;

        public RappSackCore()
        { }

        public RappSackCore(IOrganizationService service, RappSackTracerCore tracer)
        {
            this.service = service;
            this.tracer = tracer;
        }

        public void SetService(IOrganizationService service) => this.service = service;

        public void SetTracer(RappSackTracerCore tracer) => this.tracer = tracer;

        #region Tracer

        public void Trace(string message, TraceLevel level = TraceLevel.Information) => tracer.Trace(message, level);

        [Obsolete("Use Trace(string message) instead")]
        public void Log(string message) => Trace(message);

        public void Trace(Exception exception) => tracer.Trace(exception);

        public void TraceIn(string name = "") => tracer.TraceIn(name);

        public void TraceOut() => tracer.TraceOut();

        #endregion Tracer

        #region IOrganizationService

        public virtual void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Trace($"Associating {entityName} {entityId} over {relationship.SchemaName} with {relatedEntities.Count} {string.Join(", ", relatedEntities.Select(r => r.LogicalName))}");
            service.Associate(entityName, entityId, relationship, relatedEntities);
            Trace($"Associated");
        }

        public virtual Guid Create(Entity entity)
        {
            var msg = entity.Attributes.Count > 8 ? $"{entity.Attributes.Count} attributes" : $"attributes {string.Join(", ", entity.Attributes.Keys)}";
            Trace($"Creating {entity.LogicalName} with {msg}");
            var result = service.Create(entity);
            Trace($"Created {result}");
            return result;
        }

        public virtual void Delete(string entityName, Guid id)
        {
            Trace($"Deleting {entityName} {id}");
            service.Delete(entityName, id);
            Trace($"Deleted");
        }

        public virtual void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Trace($"Disassociating {entityName} {entityId} over {relationship.SchemaName} with {relatedEntities.Count} {string.Join(", ", relatedEntities.Select(r => r.LogicalName))}");
            service.Disassociate(entityName, entityId, relationship, relatedEntities);
            Trace($"Disassociated");
        }

        public virtual OrganizationResponse Execute(OrganizationRequest request)
        {
            Trace($"Executing {request.RequestName}");
            var result = service.Execute(request);
            Trace($"Executed");
            return result;
        }

        public virtual Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            Trace($"Retrieving {entityName} {id} with {columnSet.Columns.Count} attributes");
            var result = service.Retrieve(entityName, id, columnSet);
            Trace($"Retrieved");
            return result;
        }

        public virtual EntityCollection RetrieveMultiple(QueryBase query)
        {
            var queryinfo = query is QueryExpression qe ? qe.EntityName : query is QueryByAttribute qba ? qba.EntityName : query is FetchExpression ? "with fetchxml" : query.ToString();
            Trace($"Retrieving {queryinfo}");
            var result = service.RetrieveMultiple(query);
            Trace($"Retrieved {result.Entities.Count}");
            return result;
        }

        public virtual void Update(Entity entity)
        {
            var msg = entity.Attributes.Count > 8 ? $"{entity.Attributes.Count} attributes" : $"attributes {string.Join(", ", entity.Attributes.Keys)}";
            Trace($"Updating {entity.LogicalName} {entity.Id} with {msg}");
            service.Update(entity);
            Trace($"Updated");
        }

        #endregion IOrganizationService
    }
}