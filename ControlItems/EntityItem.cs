namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk;
    using Rappen.XTB.Helpers.Extensions;
    using Rappen.XTB.Helpers.Interfaces;
    using Rappen.XTB.Helpers.Serialization;
    using System;

    public class EntityItem : ICDSControlItem
    {
        #region Private Fields

        private IOrganizationService service;

        #endregion Private Fields

        #region Public Constructors

        public EntityItem(Entity entity, string format, IOrganizationService organizationService)
        {
            Entity = entity;
            Format = format;
            service = organizationService;
        }

        #endregion Public Constructors

        #region Public Properties

        public Entity Entity { get; }

        public string Format { get; set; }

        #endregion Public Properties

        #region Private Methods

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

        public string GetValue() => Entity?.Id.ToString();

        #endregion Private Methods

        #region Public Methods

        public override string ToString()
        {
            if (Entity == null)
            {
                return string.Empty;
            }
            var value = Format;
            if (string.IsNullOrWhiteSpace(value))
            {
                value = service.GetPrimaryAttribute(Entity.LogicalName)?.LogicalName ?? string.Empty;
            }
            if (!value.Contains("{{") || !value.Contains("}}"))
            {
                value = "{{" + value + "}}";
            }
            while (value.Contains("{{") && value.Contains("}}"))
            {
                var part = value.Substring(value.IndexOf("{{") + 2).Split(new string[] { "}}" }, StringSplitOptions.None)[0];
                var attribute = part;
                var format = string.Empty;
                if (part.Contains("|"))
                {
                    attribute = part.Split('|')[0];
                    format = part.Split('|')[1];
                }
                var partvalue = GetFormattedValue(Entity, service, attribute, format);
                value = value.Replace("{{" + part + "}}", partvalue);
            }
            return value;
        }

        #endregion Public Methods
    }
}
