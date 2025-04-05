using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
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
        private const int defaultchunksize = 1000;

        #region Setting up RappSack

        public RappSackCore()
        { }

        public RappSackCore(IOrganizationService service, RappSackTracerCore tracer)
        {
            SetService(service);
            SetTracer(tracer);
        }

        public void SetService(IOrganizationService service) => this.service = service;

        public void SetService(IOrganizationServiceFactory factory, Guid? userid)
        {
            SetService(factory.CreateOrganizationService(userid));
            Trace($"Service set for userid: {userid?.ToString() ?? "SYSTEM"}");
        }

        public void SetTracer(RappSackTracerCore tracer) => this.tracer = tracer;

        protected string CallerMethodName() => tracer.CallerMethodName();

        #endregion Setting up RappSack

        #region Tracer

        public void Trace(string message, TraceLevel level = TraceLevel.Information) => tracer.Trace(message, level);

        public void Trace(QueryBase query)
        {
            var fetch = string.Empty;
            if (query is FetchExpression fex)
            {
                fetch = fex.Query;
            }
            else if (query is QueryExpression qex)
            {
                try
                {
                    var response = service.Execute(new QueryExpressionToFetchXmlRequest { Query = qex }) as QueryExpressionToFetchXmlResponse;
                    fetch = response.FetchXml;
                }
                catch (Exception ex)
                {
                    Trace($"Could not write Fetch XML: {ex}", TraceLevel.Error);
                }
            }
            if (!string.IsNullOrEmpty(fetch))
            {
                Trace($"Query:\n{fetch}");
            }
        }

        public void Trace(Exception exception) => tracer.Trace(exception);

        public void TraceRaw(string message, TraceLevel level = TraceLevel.Information) => tracer.TraceRaw(message, level);

        public void TraceIn(string name = "") => tracer.TraceIn(name);

        public void TraceOut() => tracer.TraceOut();

        [Obsolete("Use Trace(string message) instead")]
        internal void Log(string message) => Trace(message);

        #endregion Tracer

        #region IOrganizationService

        public Guid Create(Entity entity) => Create(entity, new RequestParameters());

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => Retrieve(entityName, id, columnSet, new RequestParameters());

        public void Update(Entity entity) => Update(entity, new RequestParameters());

        public void Delete(string entityName, Guid id) => Delete(entityName, id, new RequestParameters());

        public OrganizationResponse Execute(OrganizationRequest request) => Execute(request, new RequestParameters());

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => Associate(entityName, entityId, relationship, relatedEntities, new RequestParameters());

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => Disassociate(entityName, entityId, relationship, relatedEntities, new RequestParameters());

        public EntityCollection RetrieveMultiple(QueryBase query) => RetrieveMultiple(query, new RequestParameters());

        #endregion IOrganizationService

        #region IOrganizationService With Params

        public virtual void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, RequestParameters parameters)
        {
            Trace($"Associating {entityName} {entityId} over {relationship.SchemaName} with {relatedEntities.Count} {string.Join(", ", relatedEntities.Select(r => r.LogicalName))}");
            if (parameters?.HasParameters == true)
            {
                var associaterequest = new AssociateRequest
                {
                    Target = new EntityReference(entityName, entityId),
                    Relationship = relationship,
                    RelatedEntities = relatedEntities
                };
                parameters.SetParameters(associaterequest);
                service.Execute(associaterequest);
            }
            else
            {
                service.Associate(entityName, entityId, relationship, relatedEntities);
            }
            Trace($"Associated");
        }

        public virtual Guid Create(Entity entity, RequestParameters parameters)
        {
            var msg = entity.Attributes.Count > 8 ? $"{entity.Attributes.Count} attributes" : $"attributes {string.Join(", ", entity.Attributes.Keys)}";
            Trace($"Creating {entity.LogicalName} with {msg}");
            if (parameters?.HasParameters == true)
            {
                var createrequest = new CreateRequest { Target = entity };
                parameters.SetParameters(createrequest);
                entity.Id = ((CreateResponse)service.Execute(createrequest)).id;
            }
            else
            {
                entity.Id = service.Create(entity);
            }
            Trace($"Created {entity.Id}");
            return entity.Id;
        }

        public virtual void Delete(string entityName, Guid id, RequestParameters parameters)
        {
            Trace($"Deleting {entityName} {id}");
            if (parameters?.HasParameters == true)
            {
                var deleterequest = new DeleteRequest { Target = new EntityReference(entityName, id) };
                parameters.SetParameters(deleterequest);
                service.Execute(deleterequest);
            }
            else
            {
                service.Delete(entityName, id);
            }
            Trace($"Deleted");
        }

        public virtual void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, RequestParameters parameters)
        {
            Trace($"Disassociating {entityName} {entityId} over {relationship.SchemaName} with {relatedEntities.Count} {string.Join(", ", relatedEntities.Select(r => r.LogicalName))}");
            if (parameters?.HasParameters == true)
            {
                var disassociaterequest = new DisassociateRequest
                {
                    Target = new EntityReference(entityName, entityId),
                    Relationship = relationship,
                    RelatedEntities = relatedEntities
                };
                parameters.SetParameters(disassociaterequest);
                service.Execute(disassociaterequest);
            }
            else
            {
                service.Disassociate(entityName, entityId, relationship, relatedEntities);
            }
            Trace($"Disassociated");
        }

        public virtual OrganizationResponse Execute(OrganizationRequest request, RequestParameters parameters)
        {
            Trace($"Executing {request.RequestName}");
            if (parameters?.HasParameters == true)
            {
                parameters.SetParameters(request);
            }
            var result = service.Execute(request);
            Trace($"Executed");
            return result;
        }

        public virtual Entity Retrieve(string entityName, Guid id, ColumnSet columnSet, RequestParameters parameters)
        {
            Trace($"Retrieving {entityName} {id} with {columnSet.Columns.Count} attributes");
            Entity result;
            if (parameters?.HasParameters == true)
            {
                var retrieverequest = new RetrieveRequest { Target = new EntityReference(entityName, id), ColumnSet = columnSet };
                parameters.SetParameters(retrieverequest);
                result = ((RetrieveResponse)service.Execute(retrieverequest)).Entity;
            }
            else
            {
                result = service.Retrieve(entityName, id, columnSet);
            }
            Trace($"Retrieved");
            return result;
        }

        public virtual EntityCollection RetrieveMultiple(QueryBase query, RequestParameters parameters)
        {
            var queryinfo = query is QueryExpression qe ? qe.EntityName : query is QueryByAttribute qba ? qba.EntityName : query is FetchExpression ? "with fetchxml" : query.ToString();
            Trace($"Retrieving {queryinfo}");
            EntityCollection result;
            if (parameters?.HasParameters == true)
            {
                var retrievemultiplerequest = new RetrieveMultipleRequest { Query = query };
                parameters.SetParameters(retrievemultiplerequest);
                result = ((RetrieveMultipleResponse)service.Execute(retrievemultiplerequest)).EntityCollection;
            }
            else
            {
                result = service.RetrieveMultiple(query);
            }
            Trace($"Retrieved {result.Entities.Count}");
            return result;
        }

        public virtual void Update(Entity entity, RequestParameters parameters)
        {
            var msg = entity.Attributes.Count > 8 ? $"{entity.Attributes.Count} attributes" : $"attributes {string.Join(", ", entity.Attributes.Keys)}";
            Trace($"Updating {entity.LogicalName} {entity.Id} with {msg}");
            if (parameters?.HasParameters == true)
            {
                var updaterequest = new UpdateRequest { Target = entity };
                parameters.SetParameters(updaterequest);
                service.Execute(updaterequest);
            }
            else
            {
                service.Update(entity);
            }
            Trace($"Updated");
        }

        #endregion IOrganizationService With Params

        #region IOrganizationService Simplified

        /// <summary>
        /// Associate an entity with an entity
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="relationship"></param>
        /// <param name="relatedEntity"></param>
        public void Associate(EntityReference reference, string relationship, EntityReference relatedEntity, RequestParameters parameters = null) =>
            Associate(reference.LogicalName, reference.Id, new Relationship(relationship), new EntityReferenceCollection { relatedEntity }, parameters);

        /// <summary>
        /// Delete an entity record.
        /// </summary>
        /// <param name="entity"></param>
        public void Delete(Entity entity, RequestParameters parameters = null)
        {
            if (entity.Id.Equals(Guid.Empty))
            {
                Trace("Cannot delete - guid is empty");
                return;
            }
            Delete(entity.LogicalName, entity.Id, parameters);
        }

        /// <summary>
        /// Disassociate an entity with an entity
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="relationship"></param>
        /// <param name="relatedEntity"></param>
        public void Disassociate(EntityReference reference, string relationship, EntityReference relatedEntity, RequestParameters parameters = null) =>
            Disassociate(reference.LogicalName, reference.Id, new Relationship(relationship), new EntityReferenceCollection { relatedEntity }, parameters);

        /// <summary>
        /// Retrieve an entity record
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Entity Retrieve(EntityReference reference, params string[] columns) => Retrieve(reference, null, columns);

        /// <summary>
        /// Retrieve an entity record
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="parameters"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Entity Retrieve(EntityReference reference, RequestParameters parameters, params string[] columns) => reference != null ?
            Retrieve(reference.LogicalName, reference.Id, new ColumnSet(columns), parameters) : null;

        /// <summary>
        /// Retrieving an EntityCollection with QueryByAttribute
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public EntityCollection RetrieveMultiple(string entity, string[] attribute, object[] value, ColumnSet columns, RequestParameters parameters = null)
        {
            var query = new QueryByAttribute(entity);
            query.Attributes.AddRange(attribute);
            query.Values.AddRange(value);
            query.ColumnSet = columns;
            return RetrieveMultiple(query, parameters);
        }

        public EntityCollection RetrieveMultipleAll(QueryBase query, RequestParameters parameters = null)
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
                tmpResult = RetrieveMultiple(query, parameters);
                Trace($"Retrieved page {pageno} with {tmpResult.Entities.Count} records");
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
            if (pageno > 1)
            {
                Trace($"Retrieved {result.Entities.Count} records" + (pageno > 1 ? $" in {pageno} pages" : ""));
            }
            return result;
        }

        public Entity RetrieveOne(QueryBase query, RequestParameters parameters = null) => RetrieveMultiple(query, parameters).Entities.FirstOrDefault();

        public Guid Save(Entity entity, RequestParameters parameters = null)
        {
            if (entity.Id.Equals(Guid.Empty))
            {
                return Create(entity, parameters);
            }
            Update(entity, parameters);
            return entity.Id;
        }

        #endregion IOrganizationService Simplified

        #region IOrgaizationService Multiple calls

        public IEnumerable<Guid> CreateMultiple(IEnumerable<Entity> entities, int chunksize = 0, RequestParameters parameters = null)
        {
            if (entities == null || !entities.Any())
            {
                return new Guid[0];
            }
            if (entities.Count() == 1)
            {
                return new[] { Create(entities.First(), parameters) };
            }
            if (chunksize == 0)
            {
                chunksize = defaultchunksize;
            }
            if (entities.Count() <= chunksize)
            {
                return CreateMultiple(entities, parameters);
            }
            var chunks = entities.Chunkit(chunksize);
            Trace($"Creating {entities.Count()} in {chunks.Count()} chunks");
            var guids = new List<Guid>();
            foreach (var chunk in chunks)
            {
                guids.AddRange(CreateMultiple(chunk, parameters));
            }
            return guids;
        }

        private IEnumerable<Guid> CreateMultiple(IEnumerable<Entity> entities, RequestParameters parameters)
        {
            if (entities?.Any() != true)
            {
                return new Guid[0];
            }
            if (entities.Count() == 1)
            {
                return new[] { Create(entities.First(), parameters) };
            }
            Trace($"Creating {entities.Count()} records");
            var entitycoll = new EntityCollection { EntityName = entities.FirstOrDefault().LogicalName };
            entitycoll.Entities.AddRange(entities);
            var request = new CreateMultipleRequest { Targets = entitycoll };
            if (parameters?.HasParameters == true)
            {
                parameters.SetParameters(request);
            }
            var result = service.Execute(request) as CreateMultipleResponse;
            Trace("Created");
            if (result?.Ids == null)
            {
                throw new InvalidPluginExecutionException("IDs are null.");
            }
            if (entitycoll.Entities.Count != result.Ids.Count())
            {
                throw new InvalidPluginExecutionException($"{entitycoll.Entities.Count} created, but {result.Ids.Count()} IDs returned.");
            }
            for (int i = 0; i < entitycoll.Entities.Count; i++)
            {
                entitycoll.Entities[i].Id = result.Ids[i];
            }
            return result.Ids;
        }

        public void UpdateMultiple(IEnumerable<Entity> entities, int chunksize = 0, RequestParameters parameters = null)
        {
            if (entities == null || !entities.Any())
            {
                return;
            }
            if (entities.Count() == 1)
            {
                Update(entities.First(), parameters);
                return;
            }
            if (chunksize == 0)
            {
                chunksize = defaultchunksize;
            }
            if (entities.Count() <= chunksize)
            {
                UpdateMultiple(entities, parameters);
                return;
            }
            var chunks = entities.Chunkit(chunksize);
            Trace($"Updating {entities.Count()} in {chunks.Count()} chunks");
            foreach (var chunk in chunks)
            {
                UpdateMultiple(chunk, parameters);
            }
        }

        private void UpdateMultiple(IEnumerable<Entity> entities, RequestParameters parameters)
        {
            if (entities?.Any() != true)
            {
                return;
            }
            if (entities.Count() == 1)
            {
                Update(entities.First(), parameters);
                return;
            }
            Trace($"Updating {entities.Count()} records");
            var entitycoll = new EntityCollection { EntityName = entities.FirstOrDefault().LogicalName };
            entitycoll.Entities.AddRange(entities);
            var request = new UpdateMultipleRequest { Targets = entitycoll };
            if (parameters?.HasParameters == true)
            {
                parameters.SetParameters(request);
            }
            service.Execute(request);
            Trace("Updated");
        }

        #endregion IOrgaizationService Multiple calls

        #region IOrganizationService Early Bound

        /// <summary>
        /// Retrieve an entity record with early bound
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T Retrieve<T>(EntityReference reference, params string[] columns) where T : Entity => Retrieve<T>(reference, null, columns);

        /// <summary>
        /// Retrieve an entity record with early bound
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T Retrieve<T>(EntityReference reference, RequestParameters parameters, params string[] columns) where T : Entity => reference != null ?
            Retrieve<T>(reference.LogicalName, reference.Id, columns) : null;

        public T Retrieve<T>(string entityName, Guid id, params string[] columns) where T : Entity => Retrieve<T>(entityName, id, null, columns);

        /// <summary>
        /// Retrieve an entity record with early bound
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityName"></param>
        /// <param name="id"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T Retrieve<T>(string entityName, Guid id, RequestParameters parameters, params string[] columns) where T : Entity =>
            Retrieve(new EntityReference(entityName, id), parameters, columns).ToEntity<T>();

        /// <summary>
        /// Retrieves a collection of entities with early bound
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public IEnumerable<T> RetrieveMultiple<T>(QueryBase query, RequestParameters parameters = null) where T : Entity => RetrieveMultiple(query, parameters).Entities.Select(x => x.ToEntity<T>());

        /// <summary>
        /// Retrieving an EntityCollection with QueryByAttribute with early bound
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public IEnumerable<T> RetrieveMultiple<T>(string entity, string[] attribute, object[] value, ColumnSet columns, RequestParameters parameters = null) where T : Entity
        {
            var query = new QueryByAttribute(entity);
            query.Attributes.AddRange(attribute);
            query.Values.AddRange(value);
            query.ColumnSet = columns;
            return RetrieveMultiple<T>(query, parameters);
        }

        public IEnumerable<T> RetrieveMultipleAll<T>(QueryBase query, RequestParameters parameters = null) where T : Entity => RetrieveMultipleAll(query, parameters).Entities.Select(x => x.ToEntity<T>());

        public T RetrieveOne<T>(QueryBase query, RequestParameters parameters = null) where T : Entity => RetrieveMultiple<T>(query, parameters).FirstOrDefault();

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

        #region Sharing Methods

        /// <summary>Retrieves all principals and their access
        /// Details: https://learn.microsoft.com/dotnet/api/microsoft.crm.sdk.messages.retrievesharedprincipalsandaccessrequest
        /// </summary>
        /// <param name="entity">The record for which to get principals</param>
        /// <returns>All principals shared to</returns>
        public IEnumerable<PrincipalAccess> GetPrincipalsAndAccess(EntityReference entity)
        {
            var result = (RetrieveSharedPrincipalsAndAccessResponse)Execute(new RetrieveSharedPrincipalsAndAccessRequest
            {
                Target = entity
            });
            Trace($"Retrieved {result.PrincipalAccesses.Count()} principals for {entity.LogicalName} {entity.Id}");
            return result.PrincipalAccesses;
        }

        /// <summary>Retrieves current AccessRights for given principal on current record</summary>
        /// <param name="entity">The record the get access rights for</param>
        /// <param name="principal">User or Team to read access for</param>
        /// <returns>Current access</returns>
        public AccessRights GetAccess(EntityReference entity, EntityReference principal)
        {
            var accessResponse = (RetrievePrincipalAccessResponse)Execute(new RetrievePrincipalAccessRequest
            {
                Principal = principal,
                Target = entity
            });
            var result = accessResponse.AccessRights;
            Trace($"Read access {result} on {entity.LogicalName} {entity.Id} for {principal.LogicalName} {principal.Name ?? principal.Id.ToString()}");
            return result;
        }

        /// <summary>Gives "rights" to "principal" for current record
        /// Details: https://docs.microsoft.com/dotnet/api/microsoft.crm.sdk.messages.grantaccessrequest
        /// </summary>
        /// <param name="entity">The entity on which the access needs to be granted</param>
        /// <param name="principal">User or Team to grant access to</param>
        /// <param name="rights">Rights to grant to user/team</param>
        public void GrantAccess(EntityReference entity, EntityReference principal, AccessRights rights)
        {
            Execute(new GrantAccessRequest()
            {
                PrincipalAccess = new PrincipalAccess()
                {
                    Principal = principal,
                    AccessMask = rights
                },
                Target = entity
            });
            Trace($"Granted {rights} on {entity.LogicalName} {entity.Id} to {principal.LogicalName} {principal.Name ?? principal.Id.ToString()}");
        }

        /// <summary>Removes access from revokee on current record
        /// Details: https://docs.microsoft.com/dotnet/api/microsoft.crm.sdk.messages.revokeaccessrequest
        /// </summary>
        /// <param name="entity">The record to remove shared from</param>
        /// <param name="principal">User or Team to revoke access from</param>
        public void RevokeAccess(EntityReference entity, EntityReference principal)
        {
            Execute(new RevokeAccessRequest()
            {
                Revokee = principal,
                Target = entity
            });
            Trace($"Revoked {principal.LogicalName} {principal.Name ?? principal.Id.ToString()} from {entity.LogicalName} {entity.Id}");
        }

        #endregion Sharing Methods
    }

    public class RequestParameters
    {
        public const string ParamNameBypassBusinessLogicExecution = "BypassBusinessLogicExecution";
        public const string ParamNameBypassBusinessLogicExecutionStepIds = "BypassBusinessLogicExecutionStepIds";
        public const string ParamNameBypassCustomPluginExecution = "BypassCustomPluginExecution";

        public bool BypassBusinessLogicExecutionSync { get; set; } = false;
        public bool BypassBusinessLogicExecutionAsync { get; set; } = false;
        public List<Guid> BypassBusinessLogicExecutionStepIds { get; set; } = new List<Guid>();
        public bool BypassCustomPluginExecution { get; set; } = false;

        public RequestParameters(bool BypassBusinessLogicExecutionSync = false, bool BypassBusinessLogicExecutionAsync = false, List<Guid> BypassBusinessLogicExecutionStepIds = null, bool BypassCustomPluginExecution = false)
        {
            this.BypassBusinessLogicExecutionSync = BypassBusinessLogicExecutionSync;
            this.BypassBusinessLogicExecutionAsync = BypassBusinessLogicExecutionAsync;
            this.BypassBusinessLogicExecutionStepIds = BypassBusinessLogicExecutionStepIds;
            this.BypassCustomPluginExecution = BypassCustomPluginExecution;
        }

        public bool HasParameters => BypassBusinessLogicExecutionSync || BypassBusinessLogicExecutionAsync || BypassBusinessLogicExecutionStepIds?.Any() == true || BypassCustomPluginExecution;

        public void SetParameters(OrganizationRequest request)
        {
            if (BypassBusinessLogicExecutionSync || BypassBusinessLogicExecutionAsync)
            {
                var syasy = new[] { BypassBusinessLogicExecutionSync ? "CustomSync" : "", BypassBusinessLogicExecutionAsync ? "CustomAsync" : "" }.Where(s => !string.IsNullOrEmpty(s));
                request.Parameters.Add(ParamNameBypassBusinessLogicExecution, string.Join(",", syasy));
            }
            else if (request.Parameters.ContainsKey(ParamNameBypassBusinessLogicExecution))
            {
                request.Parameters.Remove(ParamNameBypassBusinessLogicExecution);
            }
            if (BypassBusinessLogicExecutionStepIds?.Any() == true)
            {
                request.Parameters.Add(ParamNameBypassBusinessLogicExecutionStepIds, string.Join(",", BypassBusinessLogicExecutionStepIds.Select(i => i.ToString())));
            }
            else if (request.Parameters.ContainsKey(ParamNameBypassBusinessLogicExecutionStepIds))
            {
                request.Parameters.Remove(ParamNameBypassBusinessLogicExecutionStepIds);
            }
            if (BypassCustomPluginExecution)
            {
                request.Parameters.Add(ParamNameBypassCustomPluginExecution, true);
            }
            else if (request.Parameters.ContainsKey(ParamNameBypassCustomPluginExecution))
            {
                request.Parameters.Remove(ParamNameBypassCustomPluginExecution);
            }
        }

        public Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>();
            if (BypassBusinessLogicExecutionSync || BypassBusinessLogicExecutionAsync)
            {
                var syasy = new[] { BypassBusinessLogicExecutionSync ? "CustomSync" : "", BypassBusinessLogicExecutionAsync ? "CustomAsync" : "" }.Where(s => !string.IsNullOrEmpty(s));
                parameters.Add(ParamNameBypassBusinessLogicExecution, string.Join(",", syasy));
            }
            if (BypassBusinessLogicExecutionStepIds?.Any() == true)
            {
                parameters.Add(ParamNameBypassBusinessLogicExecutionStepIds, string.Join(",", BypassBusinessLogicExecutionStepIds.Select(i => i.ToString())));
            }
            if (BypassCustomPluginExecution)
            {
                parameters.Add(ParamNameBypassCustomPluginExecution, true);
            }
            return parameters;
        }
    }
}