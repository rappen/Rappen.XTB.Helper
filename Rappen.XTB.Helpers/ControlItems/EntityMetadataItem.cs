namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XRM.Helpers.Extensions;
    using Rappen.XTB.Helpers.Interfaces;
    using System;
    using System.Linq;

    public class EntityMetadataItem : IXRMControlItem
    {
        public bool FriendlyNames { get; set; }
        public bool IncludeLogicalName { get; set; }

        private EntityMetadataItem() { }

        public EntityMetadataItem(EntityMetadata Entity, bool friendlynames, bool includelogicalname)
        {
            if (Entity == null)
            {
                throw new ArgumentNullException("Entity");
            }
            Metadata = Entity;
            FriendlyNames = friendlynames;
            IncludeLogicalName = includelogicalname;
        }

        public EntityMetadataItem(IOrganizationService service, string entity, bool friendlynames, bool includelogicalname) : this(service.GetEntity(entity), friendlynames, includelogicalname) { }

        public EntityMetadata Metadata { get; } = null;

        public override string ToString() => FriendlyNames ? DisplayName : Metadata?.LogicalName ?? string.Empty;

        public string DisplayName => Metadata.ToDisplayName(IncludeLogicalName);

        public string CollectionDisplayName => Metadata.ToCollectionDisplayName();

        public static EntityMetadataItem Empty => new EntityMetadataItem();

        public string GetValue() => Metadata?.LogicalName ?? string.Empty;

        public OneToManyRelationshipMetadata GetRelationship(string name)
        {
            if (Metadata.OneToManyRelationships?.FirstOrDefault(r => r.SchemaName.Equals(name)) is OneToManyRelationshipMetadata rel1m)
            {
                return rel1m;
            }
            if (Metadata.ManyToOneRelationships?.FirstOrDefault(r => r.SchemaName.Equals(name)) is OneToManyRelationshipMetadata relm1)
            {
                return relm1;
            }
            return null;
        }

        public ManyToManyRelationshipMetadata GetRelationshipMM(string name)
        {
            if (Metadata.ManyToManyRelationships?.FirstOrDefault(r => r.SchemaName.Equals(name)) is ManyToManyRelationshipMetadata relmm)
            {
                return relmm;
            }
            return null;
        }
    }
}