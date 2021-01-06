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

        private IBag bag;

        #endregion Private Fields

        #region Public Constructors

        public EntityItem(Entity entity, IOrganizationService organizationService)
            : this(entity, string.Empty, organizationService) { }

        public EntityItem(Entity entity, string format, IOrganizationService organizationService)
            : this(entity, format, new GenericBag(organizationService)) { }

        public EntityItem(Entity entity, IBag bag)
            : this(entity, string.Empty, bag) { }

        public EntityItem(Entity entity, string format, IBag bag)
        {
            Entity = entity;
            Format = format;
            this.bag = bag;
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
                value = bag.Service.GetPrimaryAttribute(Entity.LogicalName)?.LogicalName ?? string.Empty;
            }
            return Entity.Substitute(bag, value);
        }

        #endregion Public Methods
    }
}
