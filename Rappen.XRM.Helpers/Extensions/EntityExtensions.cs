using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Rappen.XRM.Helpers.Extensions
{
    /// <summary>
    /// Light-weight features inspired by CintDynEntity
    /// </summary>
    public static class EntityExtensions
    {
        private static string guidtemplate = "FFFFEEEEDDDDCCCCBBBBAAAA99998888";

        private static Guid StringToGuidish(string strId, ILogger log)
        {
            Guid id = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(strId) &&
                !Guid.TryParse(strId, out id))
            {
                log.StartSection("StringToGuidish");
                log.Log($"String: {strId}");
                string template = guidtemplate;

                if (Guid.TryParse(template.Substring(0, 32 - strId.Length) + strId, out id))
                {
                    log.Log($"Composed temporary guid from template + incomplete id: {id}");
                }
                else
                {
                    log.Log($"Failed to compose temporary guid from: {strId}");
                }
                log.EndSection();
            }
            return id;
        }

        private static bool StringToBool(string value)
        {
            if (value == "0")
            {
                return false;
            }
            else if (value == "1")
            {
                return true;
            }
            else
            {
                return bool.Parse(value);
            }
        }

        #region Public Methods

        public static object AttributeToBaseType(object attribute)
        {
            if (attribute is AliasedValue aliasedvalue)
            {
                return AttributeToBaseType(aliasedvalue.Value);
            }
            else if (attribute is EntityReference entref)
            {
                if (!string.IsNullOrEmpty(entref.LogicalName) && !entref.Id.Equals(Guid.Empty))
                {
                    return entref.Id;
                }
                return null;
            }
            else if (attribute is OptionSetValue osv)
            {
                return osv.Value;
            }
            else if (attribute is OptionSetValueCollection copt)
            {
                return copt.Select(c => c.Value);
            }
            else if (attribute is Money money)
            {
                return money.Value;
            }
            else
            {
                return attribute;
            }
        }

        /// <summary>
        /// Generic method to add property with "name" and set its value of type "T" to "value"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void AddProperty<T>(this Entity entity, string name, T value)
        {
            if (entity.Attributes.Contains(name))
            {
                entity.Attributes[name] = value;
            }
            else
            {
                entity.Attributes.Add(name, value);
            }
        }

        internal static void AddProperty(this Entity entity, string attribute, string type, string value, ILogger log)
        {
            log.StartSection("AddProperty");
            log.Log($"{attribute} = \"{value}\" ({type})");
            switch (type)
            {
                case "String":
                case "Memo":
                    entity.AddProperty(attribute, value);
                    break;

                case "Int32":
                case "Integer":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.AddProperty(attribute, Int32.Parse(value));
                    }
                    break;

                case "Int64":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.AddProperty(attribute, Int64.Parse(value));
                    }
                    break;

                case "OptionSetValue":
                case "Picklist":
                case "State":
                case "Status":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.AddProperty(attribute, new OptionSetValue(int.Parse(value)));
                    }
                    break;

                case "EntityReference":
                case "Lookup":
                case "Customer":
                case "Owner":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var valueparts = value.Split(':');
                        string entityname = valueparts[0];
                        value = valueparts[1];
                        Guid refId = StringToGuidish(value, log);
                        var entref = new EntityReference(entityname, refId);
                        if (valueparts.Length > 2)
                        {
                            entref.Name = valueparts[2];
                        }
                        entity.AddProperty(attribute, entref);
                    }
                    break;

                case "DateTime":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.AddProperty(attribute, DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
                    }
                    break;

                case "Boolean":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.AddProperty(attribute, StringToBool(value));
                    }
                    break;

                case "Guid":
                case "Uniqueidentifier":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Guid uId = StringToGuidish(value, log);
                        entity.AddProperty(attribute, uId);
                    }
                    break;

                case "Decimal":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.AddProperty(attribute, decimal.Parse(value));
                    }
                    break;

                case "Money":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        entity.AddProperty(attribute, new Money(decimal.Parse(value)));
                    }
                    break;

                case "null":
                    entity.Attributes.Add(attribute, null);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Type", type, "Cannot parse attibute type");
            }

            log.EndSection();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="bag"></param>
        /// <param name="onlyId"></param>
        /// <returns></returns>
        public static Entity Clone(this Entity entity, IBag bag, bool onlyId)
        {
            var clone = new Entity(entity.LogicalName, entity.Id);
            if (!onlyId)
            {
                foreach (var attribute in entity.Attributes)
                {
                    if (!clone.Attributes.Contains(attribute.Key))
                    {
                        clone.Attributes.Add(attribute);
                    }
                }
            }
            bag.Logger.Log($"Cloned {entity.LogicalName} {entity.Id} with {entity.Attributes.Count} attributes");
            return clone;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="notnull"></param>
        /// <returns></returns>
        public static bool Contains(this Entity entity, string name, bool notnull) => entity.Attributes.Contains(name) && (!notnull || entity.Attributes[name] != null);

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bag"></param>
        /// <param name="related"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static Entity GetRelated(this Entity source, IBag bag, string related, params string[] columns) => source.GetRelated(bag, related, new ColumnSet(columns));

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bag"></param>
        /// <param name="related"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static Entity GetRelated(this Entity source, IBag bag, string related, ColumnSet columns)
        {
            bag.Logger.StartSection($"GetRelated {related} from {source.LogicalName} {source.Id}");
            Entity result = null;
            var refname = related;
            var refatt = string.Empty;
            if (refname.Contains("."))
            {
                refatt = refname.Substring(refname.IndexOf('.') + 1);
                refname = refname.Substring(0, refname.IndexOf('.'));
            }
            if (source.Attributes.Contains(refname))
            {
                EntityReference reference = null;
                if (source.Attributes[refname] is EntityReference er)
                {
                    reference = er;
                }
                else if (source.Attributes[refname] is Guid id && refname.EndsWith("id"))
                {
                    reference = new EntityReference(string.Empty, id);
                }
                if (string.IsNullOrEmpty(reference.LogicalName))
                {
                    //reference.LogicalName = Common.CintDynEntity.GetRelatedEntityNameFromLookupAttributeName(refname);
                }
                if (reference != null)
                {
                    if (refatt != "")
                    {
                        var nextref = refatt;
                        if (nextref.Contains("."))
                        {
                            nextref = nextref.Substring(0, nextref.IndexOf('.'));
                        }

                        bag.Logger.Log($"Loading {reference.LogicalName} {reference.Id} column {nextref}");
                        var cdNextRelated = bag.Service.Retrieve(reference.LogicalName, reference.Id, new ColumnSet(new string[] { nextref }));
                        if (cdNextRelated != null)
                        {
                            result = cdNextRelated.GetRelated(bag, refatt, columns);
                        }
                    }
                    else
                    {
                        result = bag.Service.Retrieve(reference.LogicalName, reference.Id, columns);
                    }
                }
            }
            else
            {
                bag.Logger.Log($"Record does not contain attribute {refname}");
            }
            if (result == null)
            {
                bag.Logger.Log("Could not load related record");
            }
            else
            {
                bag.Logger.Log($"Loaded related {result.LogicalName} {result.Id}");
            }

            bag.Logger.EndSection();
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="referencingattribute"></param>
        /// <param name="onlyactive"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static EntityCollection GetRelating(this Entity source, IBag bag, string entity, string referencingattribute, bool onlyactive, params string[] columns) => source.GetRelating(bag, entity, referencingattribute, onlyactive, null, null, new ColumnSet(columns), false);

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bag"></param>
        /// <param name="entity"></param>
        /// <param name="referencingattribute"></param>
        /// <param name="onlyactive"></param>
        /// <param name="extrafilter"></param>
        /// <param name="orders"></param>
        /// <param name="columns"></param>
        /// <param name="nolock"></param>
        /// <returns></returns>
        public static EntityCollection GetRelating(this Entity source, IBag bag, string entity, string referencingattribute, bool onlyactive, FilterExpression extrafilter, OrderExpression[] orders, ColumnSet columns, bool nolock)
        {
            bag.Logger.StartSection($"GetRelating {entity} where {referencingattribute}={source.Id} and active={onlyactive}");
            var qry = new QueryExpression(entity);
            qry.Criteria.AddCondition(referencingattribute, ConditionOperator.Equal, source.Id);
            if (onlyactive)
            {
                qry.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            }

            qry.ColumnSet = columns;
            if (extrafilter != null)
            {
                bag.Logger.Log($"Adding filter with {extrafilter.Conditions.Count} conditions");
                qry.Criteria.AddFilter(extrafilter);
            }

            if (orders != null && orders.Length > 0)
            {
                bag.Logger.Log($"Adding orders ({orders.Length})");
                qry.Orders.AddRange(orders);
            }

            qry.NoLock = nolock;
            var result = bag.Service.RetrieveMultiple(qry);
            bag.Logger.Log($"Got {result.Entities.Count} records");
            bag.Logger.EndSection();
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="bag"></param>
        /// <param name="entity2"></param>
        /// <returns></returns>
        public static Entity Merge(this Entity entity1, IBag bag, Entity entity2)
        {
            bag.Logger.StartSection($"Merge {entity1.LogicalName} {entity1.ToStringExt(bag.Service)} with {entity2.LogicalName} {entity2.ToStringExt(bag.Service)}");
            var merge = entity1.Clone(bag, false);
            foreach (var prop in entity2.Attributes)
            {
                if (!merge.Attributes.Contains(prop.Key))
                {
                    merge.Attributes.Add(prop);
                }
            }
            bag.Logger.Log($"Base entity had {entity1.Attributes.Count} attributes. Second entity {entity2.Attributes.Count}. Merged entity has {merge.Attributes.Count}");
            bag.Logger.EndSection();
            return merge;
        }

        /// <summary>
        /// Generic method to retrieve property with name "name" of type "T"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="attributename"></param>
        /// <param name="defaultvalue"></param>
        /// <returns></returns>
        public static T Property<T>(this Entity entity, string attributename, T defaultvalue) => (T)(object)(entity.Contains(attributename) && entity[attributename] is T ? (T)entity[attributename] : defaultvalue);

        /// <summary>Gets the value of a property derived to its base type</summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="def"></param>
        /// <param name="supresserrors"></param>
        /// <returns>Base type of attribute</returns>
        /// <remarks>Translates <c>AliasedValue, EntityReference, OptionSetValue and Money</c> to their underlying base types</remarks>
        public static object PropertyAsBaseType(this Entity entity, string name, object def, bool supresserrors)
        {
            if (!entity.Contains(name))
            {
                if (!supresserrors)
                {
                    throw new InvalidPluginExecutionException(string.Format("Attribute {0} not found in entity {1} {2}", name, entity.LogicalName, entity.Id));
                }
                else
                {
                    return def;
                }
            }
            return AttributeToBaseType(entity[name]);
        }

        /// <summary>Gets the value of a property as a formatted string</summary>
        /// <param name="entity"></param>
        /// <param name="bag"></param>
        /// <param name="name"></param>
        /// <param name="def"></param>
        /// <param name="supresserrors"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string PropertyAsString(this Entity entity, IBag bag, string name, string def = null, bool supresserrors = false, string format = null)
        {
            bool hasValueFormat = false;

            if (!entity.Contains(name))
            {
                if (!supresserrors)
                {
                    throw new InvalidPluginExecutionException(string.Format("Attribute {0} not found in entity {1} {2}", name, entity.LogicalName, entity.Id));
                }
                else
                {
                    return def;
                }
            }

            // Extrahera eventuella egna implementerade formatsträngar, t.ex. "<MaxLen=20>"
            var extraFormats = new List<string>();
            format = format.ExtractExtraFormatTags(extraFormats);

            string result = null;
            object oAttrValue = entity.Contains(name) ? entity[name] : null;

            if (oAttrValue != null && format?.StartsWith("<value>") == true)
            {
                hasValueFormat = true;
                format = format.Replace("<value>", "");

                oAttrValue = AttributeToBaseType(oAttrValue);
            }

            if (oAttrValue != null && !string.IsNullOrWhiteSpace(format))
            {
                if (oAttrValue is AliasedValue)
                {
                    oAttrValue = AttributeToBaseType(((AliasedValue)oAttrValue).Value);
                }

                if (format == "<entity>")
                {
                    if (oAttrValue is EntityReference entref)
                    {
                        result = entref.LogicalName;
                    }
                    else if (oAttrValue is Guid guid)
                    {
                        result = entity.LogicalName;
                    }
                }
                else if (format == "<recordurl>")
                {
                    if (oAttrValue is EntityReference entref)
                    {
                        result = bag.Service.GetEntityFormUrl(entref);
                    }
                    else if (oAttrValue is Guid guid)
                    {
                        result = bag.Service.GetEntityFormUrl(new EntityReference(entity.LogicalName, guid));
                    }
                }
                else if (oAttrValue is Money)
                {
                    decimal dAttrValue = ((Money)oAttrValue).Value;
                    result = dAttrValue.ToString(format);
                }
                else if (oAttrValue is int)
                {
                    result = ((int)oAttrValue).ToString(format);
                }
                else if (oAttrValue is decimal)
                {
                    result = ((decimal)oAttrValue).ToString(format);
                }
            }
            if (result == null)
            {
                if (oAttrValue != null && oAttrValue is EntityReference er)
                {   // Introducerat för att nyttja metadata- och entitetscache på CrmServiceProxy
                    var related = entity.GetRelated(bag, name, bag.Service.GetPrimaryAttribute(er.LogicalName).LogicalName);
                    result = Utils.EntityToString(related, bag.Service);
                }
                else if (hasValueFormat)
                {
                    if (oAttrValue is IEnumerable<int> manyint)
                    {
                        result = string.Join(";", manyint.Select(m => m.ToString()));
                    }
                    else
                    {
                        result = oAttrValue.ToString();
                    }
                }
                else
                {
                    result = entity.AttributeToString(name, def, bag.Service);
                }

                if (!string.IsNullOrWhiteSpace(format))
                {
                    DateTime tmpDateTime;
                    int tmpInt;
                    decimal tmpDecimal;
                    if (DateTime.TryParse(result, out tmpDateTime))
                    {
                        result = tmpDateTime.ToString(format);
                    }
                    else if (int.TryParse(result, out tmpInt))
                    {
                        result = tmpInt.ToString(format);
                    }
                    else if (decimal.TryParse(result.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out tmpDecimal))
                    {
                        result = tmpDecimal.ToString(format);
                    }
                    else
                    {
                        if (!format.Contains("{0}"))
                        {
                            format = "{0:" + format + "}";
                        }
                        result = string.Format(format, result);
                    }
                }
            }
            // Applicera eventuella egna implementerade formatsträngar
            foreach (var extraFormat in extraFormats)
            {
                result = result.FormatByTag(extraFormat);
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        public static void RemoveProperty(this Entity entity, string name)
        {
            if (entity.Contains(name))
            {
                entity.Attributes.Remove(name);
            }
        }

        /// <summary>
        /// Returns the name (value of Primary Attribute) of the entity. If PA is not available, the Guid is returned.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Primary Attribute value, or Primary Key</returns>
        public static string ToStringExt(this Entity entity, IOrganizationService service) => Utils.EntityToString(entity, service);

        /// <summary>Gets a readable string representation of given attribute</summary>
        /// <param name="entity">Entity containing the attribute</param>
        /// <param name="attributename">Name of the attribute</param>
        /// <returns>String value of the attribute. If the attribute is nussing, null is returned.</returns>
        public static string AttributeToString(this Entity entity, string attributename) => AttributeToString(entity, attributename, null, null);

        /// <summary>Gets a readable string representation of given attribute</summary>
        /// <param name="entity">Entity containing the attribute</param>
        /// <param name="attributename">Name of the attribute</param>
        /// <param name="service">Service to use for optionset/entityreference value retrieval</param>
        /// <returns>String value of the attribute. If the attribute is nussing, null is returned.</returns>
        public static string AttributeToString(this Entity entity, string attributename, IOrganizationService service) => AttributeToString(entity, attributename, null, service);

        /// <summary>Gets a readable string representation of given attribute</summary>
        /// <param name="entity">Entity containing the attribute</param>
        /// <param name="attributename">Name of the attribute</param>
        /// <param name="def">Default value if attribute is missing</param>
        /// <returns>String value of the attribute. If the attribute is nussing, default is returned.</returns>
        public static string AttributeToString(this Entity entity, string attributename, string def) => AttributeToString(entity, attributename, def, null);

        /// <summary>Gets a readable string representation of given attribute</summary>
        /// <param name="entity">Entity containing the attribute</param>
        /// <param name="attributename">Name of the attribute</param>
        /// <param name="def">Default value if attribute is missing</param>
        /// <param name="service">Service to use for optionset/entityreference value retrieval</param>
        /// <returns>String value of the attribute. If the attribute is nussing, default is returned.</returns>
        public static string AttributeToString(this Entity entity, string attributename, string def, IOrganizationService service)
        {
            if (!string.IsNullOrWhiteSpace(attributename) && entity.Contains(attributename))
            {
                if (entity.FormattedValues.Contains(attributename) && !string.IsNullOrEmpty(entity.FormattedValues[attributename]))
                {
                    return entity.FormattedValues[attributename];
                }
                else
                {
                    return AttributeToString(entity, entity[attributename], attributename, service);
                }
            }
            else
            {
                return def;
            }
        }

        private static string AttributeToString(Entity entity, object attribute, string attributename, IOrganizationService service)
        {
            if (attribute is AliasedValue)
            {
                return AttributeToString(entity, ((AliasedValue)attribute).Value, ((AliasedValue)attribute).AttributeLogicalName, service);
            }
            else if (attribute is EntityReference)
            {
                EntityReference refatt = (EntityReference)attribute;
                if (!string.IsNullOrEmpty(refatt.Name))
                {
                    return refatt.Name;
                }
                else if (service != null)
                {
                    var primaryattribute = service.GetPrimaryAttribute(entity.LogicalName).LogicalName;
                    var referencedentity = service.Retrieve(refatt.LogicalName, refatt.Id, new ColumnSet(primaryattribute));
                    if (referencedentity.Contains(primaryattribute))
                    {
                        return AttributeToString(referencedentity, primaryattribute, service);
                    }
                }
            }
            else if (attribute is EntityCollection && ((EntityCollection)attribute).EntityName == "activityparty")
            {
                var result = new StringBuilder();
                if (((EntityCollection)attribute).Entities.Count > 0)
                {
                    var partyAdded = false;
                    foreach (var activityparty in ((EntityCollection)attribute).Entities)
                    {
                        var party = "";
                        if (activityparty.Contains("partyid") && activityparty["partyid"] is EntityReference)
                        {
                            party = ((EntityReference)activityparty["partyid"]).Name;
                        }
                        if (string.IsNullOrEmpty(party) && activityparty.Contains("addressused"))
                        {
                            party = activityparty["addressused"].ToString();
                        }
                        if (string.IsNullOrEmpty(party))
                        {
                            party = activityparty.Id.ToString();
                        }
                        if (partyAdded)
                        {
                            result.Append(", ");
                        }
                        result.Append(party);
                        partyAdded = true;
                    }
                }
                return result.ToString();
            }
            else if (attribute is OptionSetValue optatt)
            {
                if (service != null)
                {
                    var plAMD = service.GetAttribute(entity.LogicalName, attributename) as EnumAttributeMetadata;
                    foreach (var oMD in plAMD?.OptionSet?.Options)
                    {
                        if (oMD.Value == optatt.Value)
                        {
                            return oMD.Label?.UserLocalizedLabel?.Label ?? oMD.Label.LocalizedLabels[0]?.Label;
                        }
                    }

                    return "";  // OptionSet value not found!
                }
            }
            else if (attribute is DateTime)
            {
                return ((DateTime)attribute).ToString("G");
            }
            else if (attribute is Money)
            {
                return ((Money)attribute).Value.ToString("C");
            }

            if (attribute != null)
            {
                return attribute.ToString();
            }
            else
            {
                return null;
            }
        }

        #endregion Public Methods
    }
}