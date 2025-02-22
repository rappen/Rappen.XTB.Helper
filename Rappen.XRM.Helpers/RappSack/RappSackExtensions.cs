using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Rappen.XRM.Helpers.RappSack
{
    public static class RappSackExtensions
    {
        #region Entity extensions

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