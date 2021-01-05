namespace Rappen.XTB.Helpers
{
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using Rappen.XTB.Helpers.Extensions;
    using Rappen.XTB.Helpers.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Utils
    {
        public static string ValueToString(object value) => ValueToString(value, false, false, true, null);

        public static string ValueToString(object value, bool attributetypes, bool convertqueries, bool expandcollections, IOrganizationService service, int indent = 1)
        {
            var indentstring = new string(' ', indent * 2);
            if (value == null)
            {
                return $"{indentstring}<null>";
            }
            else if (value is EntityCollection collection)
            {
                var result = $"{collection.EntityName} collection\n  Records: {collection.Entities.Count}\n  TotalRecordCount: {collection.TotalRecordCount}\n  MoreRecords: {collection.MoreRecords}\n  PagingCookie: {collection.PagingCookie}";
                if (expandcollections)
                {
                    result += $"\n{indentstring}  {string.Join($"\n{indentstring}", collection.Entities.Select(e => ValueToString(e, attributetypes, convertqueries, expandcollections, service, indent + 1)))}";
                }
                return result;
            }
            else if (value is Entity entity)
            {
                var keylen = entity.Attributes.Count > 0 ? entity.Attributes.Max(p => p.Key.Length) : 50;
                return $"{entity.LogicalName} {entity.Id}\n{indentstring}" + string.Join($"\n{indentstring}", entity.Attributes.OrderBy(a => a.Key).Select(a => $"{a.Key}{new string(' ', keylen - a.Key.Length)} = {ValueToString(a.Value, attributetypes, convertqueries, expandcollections, service, indent + 1)}"));
            }
            else if (value is ColumnSet columnset)
            {
                var columnlist = new List<string>(columnset.Columns);
                columnlist.Sort();
                return $"\n{indentstring}" + string.Join($"\n{indentstring}", columnlist);
            }
            else if (value is FetchExpression fetchexpression)
            {
                return $"{value}\n{indentstring}{fetchexpression.Query}";
            }
            else if (value is QueryExpression queryexpression && convertqueries && service != null)
            {
                var fetchxml = (service.Execute(new QueryExpressionToFetchXmlRequest { Query = queryexpression }) as QueryExpressionToFetchXmlResponse).FetchXml;
                return $"{queryexpression}\n{indentstring}{fetchxml}";
            }
            else
            {
                var result = string.Empty;
                if (value is EntityReference entityreference)
                {
                    result = $"{entityreference.LogicalName} {entityreference.Id} {entityreference.Name}";
                }
                else if (value is OptionSetValue optionsetvalue)
                {
                    result = optionsetvalue.Value.ToString();
                }
                else if (value is Money money)
                {
                    result = money.Value.ToString();
                }
                else
                {
                    result = value.ToString().Replace("\n", $"\n  {indentstring}");
                }
                return result + (attributetypes ? $" \t({value.GetType()})" : "");
            }
        }

        public static string EntityToString(Entity entity, IOrganizationService service, string Format = null)
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
            if (EntitySerializer.AttributeToBaseType(value) is DateTime dtvalue && (dtvalue).Kind == DateTimeKind.Utc)
            {
                value = dtvalue.ToLocalTime();
            }
            if (!ValueTypeIsFriendly(value) && metadata != null)
            {
                value = EntitySerializer.AttributeToString(value, metadata, format);
            }
            else
            {
                value = EntitySerializer.AttributeToBaseType(value).ToString();
            }
            return value.ToString();
        }

        private static bool ValueTypeIsFriendly(object value)
        {
            return value is Int32 || value is decimal || value is double || value is string || value is Money;
        }
    }
}
