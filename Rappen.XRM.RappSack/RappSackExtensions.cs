using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;

namespace Rappen.XRM.RappSack
{
    public static class RappSackExtensions
    {
        #region Entity extensions

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="bag"></param>
        /// <param name="onlyId"></param>
        /// <returns></returns>
        public static Entity Clone(this Entity entity, bool onlyId, RappSackCore bag = null)
        {
            if (entity == null)
            {
                return null;
            }
            var clone = new Entity(entity.LogicalName, entity.Id);

            if (!onlyId)
            {
                // Preparing all attributes except the one in which entity id is stored
                var attributes = entity.Attributes.Where(x => x.Key.ToLowerInvariant() != $"{clone.LogicalName}id".ToLowerInvariant() || (Guid)x.Value != clone.Id);
                clone.Attributes.AddRange(attributes.Where(a => !clone.Attributes.Contains(a.Key)));
            }
            bag?.Trace($"Cloned {entity.LogicalName} {entity.Id} with {entity.Attributes.Count} attributes");
            return clone;
        }

        public static T Clone<T>(this T entity, bool onlyId, RappSackCore bag = null) where T : Entity => Clone((Entity)entity, onlyId, bag).ToEntity<T>();

        /// <summary>
        /// Creates a new entity from entity1 and adding missing attributes in entity1 from entity2
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="bag"></param>
        /// <param name="entity2"></param>
        /// <returns></returns>
        public static Entity Merge(this Entity entity1, Entity entity2, RappSackCore bag = null)
        {
            if (bag != null)
            {
                bag.TraceIn($"Merge {entity1?.LogicalName} {entity1?.ToStringExt(bag)} with {entity2?.LogicalName} {entity2?.ToStringExt(bag)}");
            }
            var merge = entity1.Clone(false, bag) ?? entity2.Clone(false, bag);
            if (entity1 != null && entity2 != null)
            {
                merge.Attributes.AddRange(entity2.Attributes.Where(a => !merge.Attributes.Contains(a.Key)));
            }
            if (bag != null)
            {
                bag.Trace($"Base entity had {entity1?.Attributes?.Count} attributes. Second entity {entity2?.Attributes?.Count}. Merged entity has {merge.Attributes.Count}");
                bag.TraceOut();
            }
            return merge;
        }

        public static T Merge<T>(this T entity1, Entity entity2, RappSackCore bag = null) where T : Entity => Merge((Entity)entity1, entity2, bag).ToEntity<T>();

        /// <summary>
        /// Adds missing attributes from entity2 to entity1
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <param name="overwrite"></param>
        public static void AddAttributesFrom(this Entity entity1, Entity entity2, bool overwrite = false)
        {
            if (entity1 == null || entity2 == null)
            {
                return;
            }
            foreach (var attribute in entity2.Attributes)
            {
                if (!entity1.Attributes.Contains(attribute.Key) || overwrite)
                {
                    entity1.Attributes[attribute.Key] = attribute.Value;
                }
            }
        }

        /// <summary>
        /// Generic method to get the value of an aliased attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static T GetAliasedAttributeValue<T>(this Entity entity, string attributeName)
        {
            if (entity == null)
                return default(T);

            var fieldAliasValue = entity.GetAttributeValue<AliasedValue>(attributeName);

            if (fieldAliasValue == null)
                return default(T);

            if (fieldAliasValue.Value != null && fieldAliasValue.Value.GetType() == typeof(T))
            {
                return (T)fieldAliasValue.Value;
            }

            return default(T);
        }

        /// <summary>
        /// Generic method to retrieve property with name "name" of type "T"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="attributeName"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public static T AttributeValue<T>(this Entity entity, string attributeName, T @default) =>
            (T)(object)(entity.Contains(attributeName) && entity[attributeName] is T ? (T)entity[attributeName] : @default);

        /// <summary>
        /// Returns the name (value of Primary Attribute) of the entity. If PA is not available, the Guid is returned.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Primary Attribute value, or Primary Key</returns>
        public static string ToStringExt(this Entity entity, IOrganizationService service) => RappSackUtils.EntityToString(entity, service);

        #endregion Entity extensions

        #region Query extensions

        public static void NavigatePage(this QueryBase query, string cookie, int diffpage = 1)
        {
            if (string.IsNullOrWhiteSpace(cookie) && diffpage != 1)
            {
                //throw new Exception("Cannot navigate without a paging cookie.");
            }
            if (query is QueryExpression qex)
            {
                qex.PageInfo.PageNumber = qex.PageInfo.PageNumber + diffpage;
                qex.PageInfo.PagingCookie = cookie;
            }
            else if (query is FetchExpression fex)
            {
                var pagedoc = fex.Query.ToXml();
                if (pagedoc.SelectSingleNode("fetch") is XmlElement fetchnode)
                {
                    if (!int.TryParse(fetchnode.GetAttribute("page"), out int pageno))
                    {
                        pageno = 1;
                    }
                    pageno = pageno + diffpage;
                    fetchnode.SetAttribute("page", pageno.ToString());
                    fetchnode.SetAttribute("paging-cookie", cookie);
                    fex.Query = pagedoc.OuterXml;
                }
            }
            else
            {
                throw new Exception($"Unable to retrieve more pages, unexpected query: {query.GetType()}");
            }
        }

        public static int PageSize(this QueryBase query)
        {
            if (query is QueryExpression qex)
            {
                if (qex.PageInfo?.Count is int count)
                {
                    return count;
                }
            }
            else if (query is FetchExpression fex)
            {
                var pagedoc = fex.Query.ToXml();
                if (pagedoc.SelectSingleNode("fetch") is XmlElement fetchnode)
                {
                    if (int.TryParse(fetchnode.GetAttribute("count"), out int count))
                    {
                        return count;
                    }
                }
            }
            return 5000;
        }

        #endregion Query extensions

        #region Xml extensions

        public static XmlDocument ToXml(this string XmlString, bool IgnoreXmlErrors = true)
        {
            var xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(XmlString);
            }
            catch (XmlException e)
            {
                if (!IgnoreXmlErrors)
                {
                    throw e;
                }
            }
            return xmldoc;
        }

        #endregion Xml extensions
    }
}