namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;
    using System;
    using System.Linq;

    public class EntityMetadataItem : IXRMControlItem
    {
        public bool FriendlyNames { get; set; }

        private EntityMetadataItem() { }

        public EntityMetadataItem(EntityMetadata Entity, bool friendlynames)
        {
            if (Entity == null)
            {
                throw new ArgumentNullException("Entity");
            }
            Metadata = Entity;
            FriendlyNames = friendlynames;
        }

        public EntityMetadata Metadata { get; } = null;

        public override string ToString() => FriendlyNames ? DisplayName : Metadata?.LogicalName ?? string.Empty;

        public string DisplayName => GetDisplayName();

        public string CollectionDisplayName => GetCollectionDisplayName();

        public static EntityMetadataItem Empty => new EntityMetadataItem();

        private string GetDisplayName()
        {
            if (Metadata == null)
            {
                return string.Empty;
            }
            var result = Metadata.LogicalName;
            if (Metadata.DisplayName.UserLocalizedLabel != null)
            {
                result = Metadata.DisplayName.UserLocalizedLabel.Label;
            }
            if (result == Metadata.LogicalName && Metadata.DisplayName.LocalizedLabels.Count > 0)
            {
                result = Metadata.DisplayName.LocalizedLabels[0].Label;
            }
            return result;
        }

        private string GetCollectionDisplayName()
        {
            if (Metadata == null)
            {
                return string.Empty;
            }
            var result = Metadata.LogicalCollectionName;
            if (Metadata.DisplayCollectionName.UserLocalizedLabel != null)
            {
                result = Metadata.DisplayCollectionName.UserLocalizedLabel.Label;
            }
            if (result == Metadata.LogicalCollectionName && Metadata.DisplayCollectionName.LocalizedLabels.Count > 0)
            {
                result = Metadata.DisplayCollectionName.LocalizedLabels[0].Label;
            }
            return result;
        }

        public string GetValue()
        {
            if (Metadata == null)
            {
                return string.Empty;
            }
            return Metadata.LogicalName;
        }

        public OneToManyRelationshipMetadata GetRelationship(string name)
        {
            if (Metadata.OneToManyRelationships.FirstOrDefault(r => r.SchemaName.Equals(name)) is OneToManyRelationshipMetadata rel1m)
            {
                return rel1m;
            }
            if (Metadata.ManyToOneRelationships.FirstOrDefault(r => r.SchemaName.Equals(name)) is OneToManyRelationshipMetadata relm1)
            {
                return relm1;
            }
            return null;
        }

        public ManyToManyRelationshipMetadata GetRelationshipMM(string name)
        {
            if (Metadata.ManyToManyRelationships.FirstOrDefault(r => r.SchemaName.Equals(name)) is ManyToManyRelationshipMetadata relmm)
            {
                return relmm;
            }
            return null;
        }
    }
}