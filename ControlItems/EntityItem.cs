namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Extensions;
    using Rappen.XTB.Helpers.Interfaces;
    using Rappen.XTB.Helpers.Serialization;
    using System;

    public class EntityItem : ICDSControlItem
    {
        #region Private Fields

        protected IBag Bag;
        private Lazy<EntityMetadataItem> entitymetadata;

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
            Bag = bag;
            entitymetadata = new Lazy<EntityMetadataItem>(() => new EntityMetadataItem(bag.Service.GetEntity(entity.LogicalName), true));
        }

        #endregion Public Constructors

        #region Public Properties

        public Entity Entity { get; }

        public EntityMetadataItem Metadata => entitymetadata.Value;

        public string Format { get; set; }

        #endregion Public Properties

        #region Private Methods

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
                value = Bag.Service.GetPrimaryAttribute(Entity.LogicalName)?.LogicalName ?? string.Empty;
            }
            return Entity.Substitute(Bag, value);
        }

        #endregion Public Methods
    }
}
