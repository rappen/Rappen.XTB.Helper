using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;

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
        public static string ToStringExt(this Entity entity, IOrganizationService service) => entity.EntityToString(service);

        public static string EntityToString(this Entity entity, IOrganizationService service, string Format = null)
        {
            if (entity == null)
            {
                return string.Empty;
            }
            var value = Format;
            if (string.IsNullOrWhiteSpace(value))
            {
                value = service.GetPrimaryAttribute(entity.LogicalName)?.LogicalName ?? string.Empty;
            }
            if (!value.Contains("{{") || !value.Contains("}}"))
            {
                value = "{{" + value + "}}";
            }
            while (value.Contains("{{") && value.Contains("}}"))
            {
                var identifier = value.Substring(value.IndexOf("{{") + 2).Split(new string[] { "}}" }, StringSplitOptions.None)[0];
                var dynamicvalue = GetValueFromIdentifier(entity, service, identifier);
                value = value.Replace("{{" + identifier + "}}", dynamicvalue);
            }
            return value;
        }

        public static T ToEntity<T>(this Entity entity, string EntityAlias) where T : Entity
        {
            var logicalName = Activator.CreateInstance<T>().LogicalName;
            if (!entity.Attributes.Contains($"{EntityAlias}.{logicalName}id"))
            {
                return null;
            }
            var _entity = new Entity(logicalName, (Guid)(entity.Attributes[$"{EntityAlias}.{logicalName}id"] as AliasedValue).Value);
            var attributes = entity.Attributes.Where(x => x.Key.StartsWith(EntityAlias + ".")).ToList();
            foreach (var attribute in attributes)
            {
                _entity[attribute.Key.Replace(EntityAlias + ".", "")] = (attribute.Value as AliasedValue).Value;
            }
            try
            {
                return _entity.ToEntity<T>();
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message, ex);
            }
        }

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

        #region DateTime extensions

        public static string ToSmartString(this TimeSpan span) => span.ToSmartStringSplit().Item1 + " " + span.ToSmartStringSplit().Item2;

        /// <summary>
        /// Returns smartest string representation of a TimeSpan, separated time and unit
        /// </summary>
        /// <param name="span"></param>
        /// <returns>Item1: Span, Item2: Unit</returns>
        public static Tuple<string, string> ToSmartStringSplit(this TimeSpan span)
        {
            if (span.TotalDays >= 1)
            {
                return new Tuple<string, string>($"{span.TotalDays:0} {span.Hours:00}:{span.Minutes:00}", "days");
            }
            if (span.TotalHours >= 1)
            {
                return new Tuple<string, string>($"{span.Hours:0}:{span.Minutes:00}", "hrs");
            }
            if (span.TotalMinutes >= 1)
            {
                return new Tuple<string, string>($"{span.Minutes:0}:{span.Seconds:00}", "mins");
            }
            if (span.TotalSeconds >= 1)
            {
                return new Tuple<string, string>($"{span.Seconds:0}.{span:fff}", "secs");
            }
            return new Tuple<string, string>($"{span.TotalMilliseconds:0}", "ms");
        }

        #endregion DateTime extensions

        #region IEnumeranle extensions

        /// <summary>
        /// A list gets splitted in a list of lists with a maximum size of chunksize
        /// </summary>
        /// <remarks>
        /// Found on StackOverflow https://stackoverflow.com/a/6362642/2866704
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="chunksize"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Chunkit<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }

        #endregion IEnumeranle extensions

        #region Private methods

        private static string GetValueFromIdentifier(Entity entity, IOrganizationService service, string part)
        {
            var attribute = part;
            var format = string.Empty;
            if (part.Contains("|"))
            {
                attribute = part.Split('|')[0];
                format = part.Split('|')[1];
            }
            var partvalue = GetFormattedValue(entity, service, attribute, format);
            return partvalue;
        }

        private static string GetFormattedValue(Entity entity, IOrganizationService service, string attribute, string format)
        {
            if (!entity.Contains(attribute))
            {
                return string.Empty;
            }
            var value = entity[attribute];
            var metadata = service.GetAttribute(entity.LogicalName, attribute, value);
            if (EntityUtils.AttributeToBaseType(value) is DateTime dtvalue && (dtvalue).Kind == DateTimeKind.Utc)
            {
                value = dtvalue.ToLocalTime();
            }
            if (!ValueTypeIsFriendly(value) && metadata != null)
            {
                value = EntityUtils.AttributeToString(value, metadata, format);
            }
            else
            {
                value = EntityUtils.AttributeToBaseType(value).ToString();
            }
            return value.ToString();
        }

        private static bool ValueTypeIsFriendly(object value) => value is Int32 || value is decimal || value is double || value is string || value is Money;

        #endregion Private methods
    }
}