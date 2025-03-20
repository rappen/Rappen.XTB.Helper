using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.RappSack
{
    public static class RappSackUtils
    {
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
    }
}