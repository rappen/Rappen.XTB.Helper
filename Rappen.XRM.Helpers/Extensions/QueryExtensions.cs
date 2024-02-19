using Microsoft.Xrm.Sdk.Query;
using System;
using System.Xml;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class QueryExtensions
    {
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
                    fetchnode.SetAttribute("pagingcookie", cookie);
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
    }
}