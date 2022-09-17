using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public static EntityCollection RetrieveMultipleAll(this IOrganizationService service, QueryBase query, BackgroundWorker worker = null)
        {
            EntityCollection resultCollection = null;
            EntityCollection tmpResult;
            do
            {
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
                if (query is QueryExpression qex && tmpResult.MoreRecords)
                {
                    qex.PageInfo.PageNumber++;
                    qex.PageInfo.PagingCookie = tmpResult.PagingCookie;
                }
                worker?.ReportProgress(0, $"Retrieving records... ({resultCollection.Entities.Count})");
            }
            while (query is QueryExpression && tmpResult.MoreRecords);
            return resultCollection;
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
            var fetchDoc = new XmlDocument();
            fetchDoc.LoadXml(view.GetAttributeValue<string>(Savedquery.Fetchxml));
            var filterNodes = fetchDoc.SelectNodes("fetch/entity/filter");
            var metadata = service.GetEntity(logicalName);
            foreach (XmlNode filterNode in filterNodes)
            {
                ProcessFilter(metadata, filterNode, search);
            }
            return service.RetrieveMultiple(new FetchExpression(fetchDoc.OuterXml));
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
                var newviews = service.RetrieveMultiple(qe);
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
                var newviews = service.RetrieveMultiple(qe);
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