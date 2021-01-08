namespace Rappen.XTB.Helpers.ControlItems
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XTB.Helpers.Interfaces;
    using System;
    using System.Linq;

    public class EntityMetadataItem : ICDSControlItem
    {
        private EntityMetadata meta = null;

        public bool FriendlyNames { get; set; }

        public EntityMetadataItem(EntityMetadata Entity, bool friendlynames)
        {
            if (Entity == null)
            {
                throw new ArgumentNullException("Entity");
            }
            meta = Entity;
            FriendlyNames = friendlynames;
        }

        public override string ToString() => FriendlyNames ? DisplayName : meta.LogicalName;

        public string DisplayName => GetDisplayName();

        public string CollectionDisplayName => GetCollectionDisplayName();

        private string GetDisplayName()
        {
            var result = meta.LogicalName;
            if (meta.DisplayName.UserLocalizedLabel != null)
            {
                result = meta.DisplayName.UserLocalizedLabel.Label;
            }
            if (result == meta.LogicalName && meta.DisplayName.LocalizedLabels.Count > 0)
            {
                result = meta.DisplayName.LocalizedLabels[0].Label;
            }
            return result;
        }

        private string GetCollectionDisplayName()
        {
            var result = meta.LogicalCollectionName;
            if (meta.DisplayCollectionName.UserLocalizedLabel != null)
            {
                result = meta.DisplayCollectionName.UserLocalizedLabel.Label;
            }
            if (result == meta.LogicalCollectionName && meta.DisplayCollectionName.LocalizedLabels.Count > 0)
            {
                result = meta.DisplayCollectionName.LocalizedLabels[0].Label;
            }
            return result;
        }

        public string GetValue()
        {
            return meta.LogicalName;
        }

        public OneToManyRelationshipMetadata GetRelationship(string name)
        {
            if (meta.OneToManyRelationships.FirstOrDefault(r => r.SchemaName.Equals(name)) is OneToManyRelationshipMetadata rel1m)
            {
                return rel1m;
            }
            if (meta.ManyToOneRelationships.FirstOrDefault(r => r.SchemaName.Equals(name)) is OneToManyRelationshipMetadata relm1)
            {
                return relm1;
            }
            return null;
        }

        public ManyToManyRelationshipMetadata GetRelationshipMM(string name)
        {
            if (meta.ManyToManyRelationships.FirstOrDefault(r => r.SchemaName.Equals(name)) is ManyToManyRelationshipMetadata relmm)
            {
                return relmm;
            }
            return null;
        }
    }
}
