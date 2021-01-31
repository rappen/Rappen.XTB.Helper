namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk;
    using Rappen.XTB.Helpers.Extensions;
    using Rappen.XTB.Helpers.Interfaces;
    using System;

    public class EntityItem : IXRMControlItem
    {
        #region Private Fields

        protected IBag Bag;
        private Lazy<EntityMetadataItem> entitymetadata;

        #endregion Private Fields

        #region Public Constructors

        private EntityItem()
        {
            Entity = new Entity();
            Bag = new GenericBag(null);
        }

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

        public static EntityItem Empty => new EntityItem();

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
            return GetFormattedText(Format);
        }

        public string GetFormattedText(string format)
        {
            if (Entity == null)
            {
                return string.Empty;
            }
            if (string.IsNullOrWhiteSpace(format))
            {
                format = Bag?.Service?.GetPrimaryAttribute(Entity.LogicalName)?.LogicalName ?? string.Empty;
            }
            if (!format.Contains("{") && !format.Contains("}") && !format.Contains(" ") && !string.IsNullOrWhiteSpace(format))
            {
                format = "{" + format + "}";
            }
            return Entity.Substitute(Bag, format);
        }

        #endregion Public Methods
    }
}