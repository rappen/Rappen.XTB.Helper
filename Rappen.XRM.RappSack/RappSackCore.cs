using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.RappSack
{
    public abstract class RappSackCore : IOrganizationService
    {
        private IOrganizationService service;
        private RappSackTracerCore tracer;

        #region Setting up RappSack

        public RappSackCore()
        { }

        public RappSackCore(IOrganizationService service, RappSackTracerCore tracer)
        {
            this.service = service;
            this.tracer = tracer;
        }

        public void SetService(IOrganizationService service) => this.service = service;

        public void SetTracer(RappSackTracerCore tracer) => this.tracer = tracer;

        #endregion Setting up RappSack

        #region Tracer

        public void Trace(string message, TraceLevel level = TraceLevel.Information) => tracer.Trace(message, level);

        public void Trace(Exception exception) => tracer.Trace(exception);

        public void TraceIn(string name = "") => tracer.TraceIn(name);

        public void TraceOut() => tracer.TraceOut();

        [Obsolete("Use Trace(string message) instead")]
        public void Log(string message) => Trace(message);

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

        #region IOrganizationService Simplified

        /// <summary>
        /// Associate an entity with an entity
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="relationship"></param>
        /// <param name="relatedEntity"></param>
        public void Associate(EntityReference reference, string relationship, EntityReference relatedEntity) =>
            Associate(reference.LogicalName, reference.Id, new Relationship(relationship), new EntityReferenceCollection { relatedEntity });

        /// <summary>
        /// Delete an entity record.
        /// </summary>
        /// <param name="entity"></param>
        public void Delete(Entity entity)
        {
            if (entity.Id.Equals(Guid.Empty))
            {
                Trace("Cannot delete - guid is empty");
                return;
            }
            Delete(entity.LogicalName, entity.Id);
        }

        /// <summary>
        /// Disassociate an entity with an entity
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="relationship"></param>
        /// <param name="relatedEntity"></param>
        public void Disassociate(EntityReference reference, string relationship, EntityReference relatedEntity) =>
            Disassociate(reference.LogicalName, reference.Id, new Relationship(relationship), new EntityReferenceCollection { relatedEntity });

        /// <summary>
        /// Retrieve an entity record
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Entity Retrieve(EntityReference reference, params string[] columns) => reference != null ?
            Retrieve(reference.LogicalName, reference.Id, new ColumnSet(columns)) : null;

        /// <summary>
        /// Retrieving an EntityCollection with QueryByAttribute
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public EntityCollection RetrieveMultiple(string entity, string[] attribute, object[] value, ColumnSet columns)
        {
            var query = new QueryByAttribute(entity);
            query.Attributes.AddRange(attribute);
            query.Values.AddRange(value);
            query.ColumnSet = columns;
            return RetrieveMultiple(query);
        }

        public EntityCollection RetrieveMultipleAll(QueryBase query)
        {
            if (!(query is FetchExpression || query is QueryExpression))
            {
                throw new ArgumentException($"Query has to be FetchExpression or QueryExpression. Type is now: {query.GetType()}");
            }
            var queryinfo = query is QueryExpression qe ? qe.EntityName : query is QueryByAttribute qba ? qba.EntityName : query is FetchExpression ? "with fetchxml" : query.ToString();
            Trace($"Retrieving All {queryinfo}");

            EntityCollection result = null;
            EntityCollection tmpResult = null;
            var pageno = 0;
            if (query is QueryExpression queryex && queryex.PageInfo.PageNumber == 0 && queryex.TopCount == null)
            {
                queryex.PageInfo.PageNumber = 1;
            }
            do
            {
                pageno++;
                if (tmpResult?.MoreRecords == true)
                {
                    query.NavigatePage(tmpResult.PagingCookie);
                }
                tmpResult = service.RetrieveMultiple(query);
                if (result == null)
                {
                    result = tmpResult;
                }
                else
                {
                    result.Entities.AddRange(tmpResult.Entities);
                    result.MoreRecords = tmpResult.MoreRecords;
                    result.PagingCookie = tmpResult.PagingCookie;
                    result.TotalRecordCount = tmpResult.TotalRecordCount;
                    result.TotalRecordCountLimitExceeded = tmpResult.TotalRecordCountLimitExceeded;
                }
            }
            while (tmpResult.MoreRecords);

            Trace($"Retrieved {result.Entities.Count} records" + (pageno > 1 ? $" in {pageno} pages" : ""));
            return result;
        }

        public Entity RetrieveOne(QueryBase query) => RetrieveMultiple(query).Entities.FirstOrDefault();

        public Guid Save(Entity entity)
        {
            if (entity.Id.Equals(Guid.Empty))
            {
                return Create(entity);
            }
            Update(entity);
            return entity.Id;
        }

        #endregion IOrganizationService Simplified

        #region IOrganizationService Early Bound

        /// <summary>
        /// Retrieve an entity record with early bound
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T Retrieve<T>(EntityReference reference, params string[] columns) where T : Entity => reference != null ?
            Retrieve<T>(reference.LogicalName, reference.Id, columns) : null;

        /// <summary>
        /// Retrieve an entity record with early bound
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T Retrieve<T>(string entityName, Guid id, params string[] columns) where T : Entity =>
            Retrieve(entityName, id, new ColumnSet(columns)).ToEntity<T>();

        /// <summary>
        /// Retrieves a collection of entities with early bound
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public IEnumerable<T> RetrieveMultiple<T>(QueryBase query) where T : Entity => RetrieveMultiple(query).Entities.Select(x => x.ToEntity<T>());

        /// <summary>
        /// Retrieving an EntityCollection with QueryByAttribute with early bound
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public IEnumerable<T> RetrieveMultiple<T>(string entity, string[] attribute, object[] value, ColumnSet columns) where T : Entity
        {
            var query = new QueryByAttribute(entity);
            query.Attributes.AddRange(attribute);
            query.Values.AddRange(value);
            query.ColumnSet = columns;
            return RetrieveMultiple<T>(query);
        }

        public IEnumerable<T> RetrieveMultipleAll<T>(QueryBase query) where T : Entity => RetrieveMultipleAll(query).Entities.Select(x => x.ToEntity<T>());

        public T RetrieveOne<T>(QueryBase query) where T : Entity => RetrieveMultiple<T>(query).FirstOrDefault();

        #endregion IOrganizationService Early Bound

        #region Environment Variables

        public string GetEnvironmentVariableValue(string variablename, bool throwifnotfound)
        {
            TraceIn("GetEnvironmentVariableValue");
            Trace($"Variable Name: {variablename}");
            var variablevalue = GetEnvironmentVariableValue(variablename);
            if (variablevalue == null)
            {
                if (throwifnotfound)
                {
                    TraceOut();
                    throw new InvalidPluginExecutionException($"Could not found environment variable {variablename}");
                }
                Trace("Not found");
                TraceOut();
                return string.Empty;
            }
            string result;
            if (variablevalue.GetAttributeValue<AliasedValue>("EV.value") is AliasedValue value)
            {
                //
                result = value.Value.ToString();
                Trace($"Found Environment Variable value: {result}");
            }
            else
            {
                result = variablevalue.AttributeValue(EnvironmentVariableDefinition.DefaultValue, string.Empty);
                Trace($"Found Environment Variable default: {result}");
            }
            TraceOut();
            return result;
        }

        public T GetEnvironmentVariableValue<T>(string variablename, bool throwifnotfound)
        {
            var value = GetEnvironmentVariableValue(variablename, throwifnotfound);
            var type = typeof(T);
            if (value is T t)
            {
                return t;
            }
            if (type == typeof(Guid) && Guid.TryParse(value, out var guid))
            {
                return (T)(object)guid;
            }
            if (type == typeof(int) && int.TryParse(value, out var integer))
            {
                return (T)(object)integer;
            }
            if (type == typeof(decimal) && decimal.TryParse(value, out var decimalvalue))
            {
                return (T)(object)decimalvalue;
            }
            if (type == typeof(bool) && bool.TryParse(value, out var boolean))
            {
                return (T)(object)boolean;
            }
            if (type == typeof(DateTime) && DateTime.TryParse(value, out var datetime))
            {
                return (T)(object)datetime;
            }
            if (throwifnotfound)
            {
                throw new InvalidPluginExecutionException($"Environment Variable Value '{variablename}' is not a valid {type.Name}.");
            }
            return default(T);
        }

        /// <summary>
        /// Getting an environment variable value with a default value if not found.
        /// NOTE: This does not work for getting bools
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="variablename"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public T GetEnvironmentVariableValue<T>(string variablename, T defaultvalue)
        {
            TraceIn("GetEnvironmentVariableValue with default");
            T value;
            try
            {
                value = GetEnvironmentVariableValue<T>(variablename, true);
            }
            catch (InvalidPluginExecutionException)
            {
                value = defaultvalue;
                Log($"Using default value: {value}");
            }
            TraceOut();
            return value;
        }

        private Entity GetEnvironmentVariableValue(string variablename)
        {
            var qe = new QueryExpression(EnvironmentVariableDefinition.EntityName);
            qe.ColumnSet.AddColumns(EnvironmentVariableDefinition.PrimaryName, EnvironmentVariableDefinition
                .DefaultValue);
            qe.Criteria.AddCondition(EnvironmentVariableDefinition.PrimaryName, ConditionOperator.Equal, variablename);
            var ev = qe.AddLink(EnvironmentVariableValue.EntityName, EnvironmentVariableValue.EnvironmentVariableDefinitionId, EnvironmentVariableDefinition.PrimaryKey, JoinOperator.LeftOuter);
            ev.EntityAlias = "EV";
            ev.Columns.AddColumns(EnvironmentVariableValue.PrimaryKey, EnvironmentVariableValue.Value);
            var variablevalues = RetrieveOne(qe);
            return variablevalues;
        }

        public void SetEnvironmentVariableValue(string variablename, string value)
        {
            TraceIn("SetEnvironmentVariableValue");
            Trace($"Variable: {variablename} Value: {value}");
            var variablevalue = GetEnvironmentVariableValue(variablename);
            if (variablevalue == null)
            {
                throw new InvalidPluginExecutionException($"Could not found environment variable {variablename}");
            }
            Entity valuerecord;
            if (variablevalue.GetAliasedAttributeValue<Guid>($"EV.{EnvironmentVariableValue.PrimaryKey}") is Guid valueid)
            {
                valuerecord = new Entity(EnvironmentVariableValue.EntityName, valueid);
            }
            else
            {
                valuerecord = new Entity(EnvironmentVariableValue.EntityName);
            }
            valuerecord[EnvironmentVariableValue.Value] = value;
            Save(valuerecord);
            TraceOut();
        }

        #endregion Environment Variables
    }
}