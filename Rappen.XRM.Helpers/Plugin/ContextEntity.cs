using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace Rappen.XRM.Helpers.Plugin
{
    public class ContextEntity
    {
        private IPluginExecutionContext5 context;
        private string preImageName;
        private string postImageName;

        public int Index;

        public ContextEntity(IPluginExecutionContext5 context, string preImageName = null, string postImageName = null, int index = -1)
        {
            this.context = context;
            this.preImageName = preImageName;
            this.postImageName = postImageName;
            Index = index;
        }

        public Entity this[ContextEntityType type]
        {
            get
            {
                switch (type)
                {
                    case ContextEntityType.Target:
                        if (Index < 0)
                        {
                            if (context.InputParameters.TryGetValue(ParameterName.Target, out Entity target))
                            {
                                return target;
                            }
                            if (context.InputParameters.TryGetValue(ParameterName.Target, out EntityReference reference))
                            {
                                return new Entity(reference.LogicalName, reference.Id);
                            }
                        }
                        else
                        {
                            if (context.InputParameters.TryGetValue(ParameterName.Targets, out EntityCollection targets) &&
                                targets.Entities.Count > Index)
                            {
                                return targets.Entities[Index];
                            }
                        }
                        break;

                    case ContextEntityType.PreImage:
                        if (Index < 0)
                        {
                            if (context.PreEntityImages.Count > 0)
                            {
                                return context.PreEntityImages.FirstOrDefault().Value;
                            }
                        }
                        else
                        {
                            if (context.InputParameters.TryGetValue(ParameterName.Targets, out EntityCollection entities) &&
                                context.PreEntityImagesCollection?.Length == entities.Entities.Count() &&
                                context.PreEntityImagesCollection.Length > Index)
                            {
                                return context.PreEntityImagesCollection[Index]
                                    .FirstOrDefault(pre => string.IsNullOrEmpty(preImageName) ? !pre.Key.Equals("PreBusinessEntity") : pre.Key.Equals(preImageName)).Value;
                            }
                        }
                        break;

                    case ContextEntityType.PostImage:
                        if (Index < 0)
                        {
                            if (context.PostEntityImages.Count > 0)
                            {
                                return context.PostEntityImages.FirstOrDefault().Value;
                            }
                        }
                        else
                        {
                            if (context.InputParameters.TryGetValue(ParameterName.Targets, out EntityCollection entities) &&
                                context.PostEntityImagesCollection?.Length == entities.Entities.Count() &&
                                context.PostEntityImagesCollection.Length > Index)
                            {
                                return context.PostEntityImagesCollection[Index]
                                    .FirstOrDefault(post => string.IsNullOrEmpty(postImageName) ? !post.Key.Equals("PostBusinessEntity") : post.Key.Equals(postImageName)).Value;
                            }
                        }
                        break;

                    case ContextEntityType.Complete:
                        return this[ContextEntityType.Target].Merge(this[ContextEntityType.PostImage]).Merge(this[ContextEntityType.PreImage]);
                }
                return null;
            }
        }
    }

    public static class ContextEntityExtensions
    {
        public static Entity Merge(this Entity entity1, Entity entity2)
        {
            var merge = entity1.Clone(false) ?? entity2.Clone(false);
            if (entity1 != null && entity2 != null)
            {
                merge.Attributes.AddRange(entity2.Attributes.Where(a => !merge.Attributes.Contains(a.Key)));
            }
            return merge;
        }

        public static Entity Clone(this Entity entity, bool onlyId)
        {
            if (entity == null)
            {
                return null;
            }
            var clone = new Entity(entity.LogicalName, entity.Id);

            if (!onlyId)
            {
                // Preparing all attributes except the one in which entity id is stored
                var attributes = entity.Attributes.Where(x => x.Key.ToLowerInvariant() != $"{clone.LogicalName}id".ToLowerInvariant() || (Guid)x.Value != clone.Id);
                clone.Attributes.AddRange(attributes.Where(a => !clone.Attributes.Contains(a.Key)));
            }
            return clone;
        }
    }

    public enum ContextEntityType
    {
        Target,
        PreImage,
        PostImage,
        Complete
    }
}