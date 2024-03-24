using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Xml;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class ServiceExtensions
    {
        private static Dictionary<IOrganizationService, OrganizationDetail> organizations = new Dictionary<IOrganizationService, OrganizationDetail>();
        private const int ViewType_QuickFind = 4;
        private const string url_form_template = "{0}/main.aspx?pagetype=entityrecord&etn={1}&id={2}";

        #region Public Methods

        public static OrganizationDetail GetOrganizationDetail(this IOrganizationService service)
        {
            if (!organizations.TryGetValue(service, out var orgdetail))
            {
                orgdetail = ((RetrieveCurrentOrganizationResponse)service.Execute(new RetrieveCurrentOrganizationRequest())).Detail;
                organizations.Add(service, orgdetail);
            }
            return orgdetail;
        }

        public static string GetOrganizationUrl(this IOrganizationService service)
        {
            var orgdetail = GetOrganizationDetail(service);
            return orgdetail.Endpoints[EndpointType.WebApplication];
        }

        public static string GetEntityFormUrl(this IOrganizationService service, EntityReference entity)
        {
            var webapp = service.GetOrganizationUrl();
            if (webapp.EndsWith("/"))
            {
                webapp = webapp.Substring(0, webapp.Length - 1);
            }
            var result = string.Format(url_form_template, webapp, entity.LogicalName, entity.Id);
            return result;
        }

        /// <summary>
        /// Retrieving ALL records from Dataverse
        /// </summary>
        /// <param name="service"></param>
        /// <param name="fetch"></param>
        /// <param name="worker"></param>
        /// <param name="eventargs"></param>
        /// <param name="message">
        /// Progress message send before each page retrieving.
        /// Possible tokens:
        ///   {retrieving} - which records we are now retrieving
        ///   {page} - which page with are retrieving
        ///   {pagesize} - the size of the page to retrieve
        ///   {time} - how much time it has taken
        ///   {records} - retrieved records until now
        ///   {timeperrecord} - avarage of time to retrieve each record
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static EntityCollection RetrieveMultipleAll(this IOrganizationService service, string fetch, BackgroundWorker worker, DoWorkEventArgs eventargs, string message) => RetrieveMultipleAll(service, new FetchExpression(fetch), worker, eventargs, message, false);

        /// <summary>
        /// Retrieving ALL records from Dataverse
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static EntityCollection RetrieveMultipleAll(this IOrganizationService service, QueryBase query) => RetrieveMultipleAll(service, query, null, null, null, false);

        /// <summary>
        /// Retrieving ALL records from Dataverse
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="worker"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static EntityCollection RetrieveMultipleAll(this IOrganizationService service, QueryBase query, BackgroundWorker worker) => RetrieveMultipleAll(service, query, worker, null, null, false);

        /// <summary>
        /// Retrieving ALL records from Dataverse
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="worker"></param>
        /// <param name="message">
        /// Progress message send before each page retrieving.
        /// Possible tokens:
        ///   {retrieving} - which records we are now retrieving
        ///   {page} - which page with are retrieving
        ///   {pagesize} - the size of the page to retrieve
        ///   {time} - how much time it has taken
        ///   {records} - retrieved records until now
        ///   {timeperrecord} - avarage of time to retrieve each record
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static EntityCollection RetrieveMultipleAll(this IOrganizationService service, QueryBase query, BackgroundWorker worker, string message) => RetrieveMultipleAll(service, query, worker, null, message, false);

        /// <summary>
        /// Retrieving ALL records from Dataverse
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="worker"></param>
        /// <param name="eventargs"></param>
        /// <param name="message">
        /// Progress message send before each page retrieving.
        /// Possible tokens:
        ///   {retrieving} - which records we are now retrieving
        ///   {page} - which page with are retrieving
        ///   {pagesize} - the size of the page to retrieve
        ///   {time} - how much time it has taken
        ///   {records} - retrieved records until now
        ///   {timeperrecord} - avarage of time to retrieve each record
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static EntityCollection RetrieveMultipleAll(this IOrganizationService service, QueryBase query, BackgroundWorker worker, DoWorkEventArgs eventargs, string message, bool showMessageOnFirstPage)
        {
            if (!(query is FetchExpression || query is QueryExpression))
            {
                throw new ArgumentException($"Query has to be FetchExpression or QueryExpression. Type is now: {query.GetType()}");
            }
            EntityCollection resultCollection = null;
            EntityCollection tmpResult = null;
            if (string.IsNullOrEmpty(message))
            {
                message = "Retrieving records {retrieving} on page {page}\nRetrieved {records} in {time}";
            }
            if (query is QueryExpression queryex && queryex.PageInfo.PageNumber == 0 && queryex.TopCount == null)
            {
                queryex.PageInfo.PageNumber = 1;
            }
            var pagesize = query.PageSize();
            var page = 0;
            var sw = Stopwatch.StartNew();
            do
            {
                page++;
                if (page != 1 || showMessageOnFirstPage)
                {
                    worker?.ReportProgress(0, GetProgress(message, resultCollection?.Entities?.Count ?? 0, pagesize, page, sw));
                }
                if (worker?.CancellationPending == true && eventargs != null)
                {
                    eventargs.Cancel = true;
                    break;
                }
                if (tmpResult?.MoreRecords == true)
                {
                    query.NavigatePage(tmpResult.PagingCookie);
                }
                tmpResult = service.RetrieveMultiple(query);
                if (resultCollection == null)
                {
                    resultCollection = tmpResult;
                }
                else
                {
                    resultCollection.Entities.AddRange(tmpResult.Entities);
                    resultCollection.MoreRecords = tmpResult.MoreRecords;
                    resultCollection.PagingCookie = tmpResult.PagingCookie;
                    resultCollection.TotalRecordCount = tmpResult.TotalRecordCount;
                    resultCollection.TotalRecordCountLimitExceeded = tmpResult.TotalRecordCountLimitExceeded;
                }
            }
            while (tmpResult.MoreRecords && eventargs?.Cancel != true);
            return resultCollection;
        }

        private static string GetProgress(string message, int retrievedrecords, int pagesize, int page, Stopwatch sw)
        {
            return message.Contains("{0}") ?
                string.Format(message, retrievedrecords) :
                message
                    .Replace("{retrieving}", $"{retrievedrecords + 1}-{retrievedrecords + pagesize}")
                    .Replace("{page}", $"{page}")
                    .Replace("{pagesize}", $"{pagesize}")
                    .Replace("{time}", sw.Elapsed.ToSmartString())
                    .Replace("{records}", $"{retrievedrecords}")
                    .Replace("{timeperrecord}", retrievedrecords > 0 ? (sw.Elapsed.TotalMilliseconds / retrievedrecords).MillisecondToSmartString() : "?");
        }

        /// <summary>
        /// Detect whether a specified message is supported for the specified table.
        /// </summary>
        /// <param name="service">The IOrganizationService instance.</param>
        /// <param name="entityLogicalName">The logical name of the table.</param>
        /// <param name="messageName">The name of the message.</param>
        /// <returns>True/False</returns>
        /// <remarks>Example code from: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bulk-operations?tabs=sdk#availability-with-standard-tables</remarks>
        public static bool IsMessageAvailable(this IOrganizationService service, string entityLogicalName, string messageName, BackgroundWorker worker = null)
        {
            var query = new QueryExpression("sdkmessagefilter")
            {
                ColumnSet = new ColumnSet("sdkmessagefilterid"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = {
                        new ConditionExpression(
                            attributeName:"primaryobjecttypecode",
                            conditionOperator: ConditionOperator.Equal,
                            value: entityLogicalName)
                    }
                },
                LinkEntities = {
                    new LinkEntity(
                        linkFromEntityName:"sdkmessagefilter",
                        linkToEntityName:"sdkmessage",
                        linkFromAttributeName:"sdkmessageid",
                        linkToAttributeName:"sdkmessageid",
                        joinOperator: JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression(LogicalOperator.And) {
                            Conditions = {
                                new ConditionExpression(
                                    attributeName:"name",
                                    conditionOperator: ConditionOperator.Equal,
                                    value: messageName)
                                }
                        }
                    }
                }
            };
            worker?.ReportProgress(0, $"Checking message {messageName} on {entityLogicalName}...");
            try
            {
                var entityCollection = service.RetrieveMultipleAll(query);
                return entityCollection.Entities.Count.Equals(1);
            }
            catch
            {
                return false;
            }
        }

        public static IEnumerable<string> MessagesByEntity(this IOrganizationService service, string entityLogicalName, BackgroundWorker worker = null)
        {
            var query = new QueryExpression("sdkmessage")
            {
                ColumnSet = new ColumnSet("name"),
                LinkEntities =
                {
                    new LinkEntity("sdkmessage", "sdkmessagefilter", "sdkmessageid", "sdkmessageid", JoinOperator.Inner)
                    {
                        LinkCriteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("primaryobjecttypecode", ConditionOperator.Equal, entityLogicalName)
                            }
                        }
                    }
                }
            };
            worker?.ReportProgress(0, $"Checking messages on {entityLogicalName}...");
            try
            {
                var entityCollection = service.RetrieveMultipleAll(query);
                return entityCollection.Entities.Select(e => e.TryGetAttributeValue("name", out string message) ? message : null).Where(m => !string.IsNullOrEmpty(m)).ToArray();
            }
            catch
            {
                return null;
            }
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Runs a Quick Find search
        /// </summary>
        /// <param name="service">The <see cref="IOrganizationService"/> to use to run the query</param>
        /// <param name="logicalName">The logical name of the entity to search</param>
        /// <param name="view">The definition of the Quick Find view to use</param>
        /// <param name="search">The value to search for</param>
        /// <returns>A list of matching record</returns>
        internal static EntityCollection ExecuteQuickFind(this IOrganizationService service, string logicalName, Entity view, string search)
        {
            if (service == null)
            {
                return null;
            }
            var fetchDoc = view.GetAttributeValue<string>(Savedquery.Fetchxml).ToXml();
            var filterNodes = fetchDoc.SelectNodes("fetch/entity/filter");
            var metadata = service.GetEntity(logicalName);
            foreach (XmlNode filterNode in filterNodes)
            {
                ProcessFilter(metadata, filterNode, search);
            }
            return service.RetrieveMultipleAll(new FetchExpression(fetchDoc.OuterXml));
        }

        internal static EntityCollection RetrieveSystemViews(this IOrganizationService service, string logicalname, bool quickfind)
        {
            if (service == null)
            {
                return null;
            }
            var qe = new QueryExpression(Savedquery.EntityName);
            qe.ColumnSet.AddColumns(Savedquery.PrimaryName, Savedquery.Fetchxml, Savedquery.Layoutxml, Savedquery.QueryType, Savedquery.Isquickfindquery);
            qe.Criteria.AddCondition(Savedquery.Fetchxml, ConditionOperator.NotNull);
            qe.Criteria.AddCondition(Savedquery.Layoutxml, ConditionOperator.NotNull);
            qe.Criteria.AddCondition(Savedquery.ReturnedTypeCode, ConditionOperator.Equal, logicalname);
            qe.Criteria.AddCondition(Savedquery.QueryType, quickfind ? ConditionOperator.Equal : ConditionOperator.NotEqual, ViewType_QuickFind);
            try
            {
                var newviews = service.RetrieveMultipleAll(qe);
                return newviews;
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                return null;
            }
        }

        internal static EntityCollection RetrievePersonalViews(this IOrganizationService service, string logicalname)
        {
            if (service == null)
            {
                return null;
            }
            var qe = new QueryExpression(UserQuery.EntityName);
            qe.ColumnSet.AddColumns(UserQuery.PrimaryName, UserQuery.Fetchxml, UserQuery.Layoutxml, UserQuery.QueryType);
            qe.Criteria.AddCondition(UserQuery.Fetchxml, ConditionOperator.NotNull);
            qe.Criteria.AddCondition(UserQuery.Layoutxml, ConditionOperator.NotNull);
            qe.Criteria.AddCondition(UserQuery.ReturnedTypeCode, ConditionOperator.Equal, logicalname);
            try
            {
                var newviews = service.RetrieveMultipleAll(qe);
                return newviews;
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                return null;
            }
        }

        #endregion Internal Methods

        #region Private Methods

        private static void ProcessFilter(EntityMetadata metadata, XmlNode node, string searchTerm)
        {
            foreach (XmlNode condition in node.SelectNodes("condition"))
            {
                if (condition.Attributes["value"]?.Value?.StartsWith("{") != true)
                {
                    continue;
                }
                var attr = metadata.Attributes.First(a => a.LogicalName == condition.Attributes["attribute"].Value);

                #region Manage each attribute type

                switch (attr.AttributeType.Value)
                {
                    case AttributeTypeCode.Memo:
                    case AttributeTypeCode.String:
                        {
                            condition.Attributes["value"].Value = searchTerm.Replace("*", "%") + "%";
                        }
                        break;

                    case AttributeTypeCode.Boolean:
                        {
                            if (searchTerm != "0" && searchTerm != "1")
                            {
                                node.RemoveChild(condition);
                                continue;
                            }

                            condition.Attributes["value"].Value = (searchTerm == "1").ToString();
                        }
                        break;

                    case AttributeTypeCode.Customer:
                    case AttributeTypeCode.Lookup:
                    case AttributeTypeCode.Owner:
                        {
                            if (
                                metadata.Attributes.FirstOrDefault(
                                    a => a.LogicalName == condition.Attributes["attribute"].Value + "name") == null)
                            {
                                node.RemoveChild(condition);

                                continue;
                            }

                            condition.Attributes["attribute"].Value += "name";
                            condition.Attributes["value"].Value = searchTerm.Replace("*", "%") + "%";
                        }
                        break;

                    case AttributeTypeCode.DateTime:
                        {
                            DateTime dt;
                            if (!DateTime.TryParse(searchTerm, out dt))
                            {
                                condition.Attributes["value"].Value = new DateTime(1754, 1, 1).ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                condition.Attributes["value"].Value = dt.ToString("yyyy-MM-dd");
                            }
                        }
                        break;

                    case AttributeTypeCode.Decimal:
                    case AttributeTypeCode.Double:
                    case AttributeTypeCode.Money:
                        {
                            decimal d;
                            if (!decimal.TryParse(searchTerm, out d))
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = d.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;

                    case AttributeTypeCode.Integer:
                        {
                            int d;
                            if (!int.TryParse(searchTerm, out d))
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = d.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;

                    case AttributeTypeCode.Picklist:
                        {
                            var opt = ((PicklistAttributeMetadata)attr).OptionSet.Options.FirstOrDefault(
                                o => o.Label.UserLocalizedLabel.Label == searchTerm);

                            if (opt == null)
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = opt.Value.Value.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;

                    case AttributeTypeCode.State:
                        {
                            var opt = ((StateAttributeMetadata)attr).OptionSet.Options.FirstOrDefault(
                                o => o.Label.UserLocalizedLabel.Label == searchTerm);

                            if (opt == null)
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = opt.Value.Value.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;

                    case AttributeTypeCode.Status:
                        {
                            var opt = ((StatusAttributeMetadata)attr).OptionSet.Options.FirstOrDefault(
                                o => o.Label.UserLocalizedLabel.Label == searchTerm);

                            if (opt == null)
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = opt.Value.Value.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;
                }

                #endregion Manage each attribute type
            }

            foreach (XmlNode filter in node.SelectNodes("filter"))
            {
                ProcessFilter(metadata, filter, searchTerm);
            }
        }

        #endregion Private Methods
    }
}