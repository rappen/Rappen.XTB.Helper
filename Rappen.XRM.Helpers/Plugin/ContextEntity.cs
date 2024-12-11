using Microsoft.Xrm.Sdk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.Helpers.Plugin
{
    public class ContextEntity
    {
        private IPluginExecutionContext5 context;
        private string preImageName;
        private string postImageName;

        public int Index;

        /// <summary>
        /// Contructor of ContextEntity to access Target, PreImage, PostImage and Complete
        /// </summary>
        /// <param name="context">IPluginExecutionContext5 is all you need to use this class</param>
        /// <param name="preImageName">If you have a specific pre image name, enter it, otherwise to first one will be used</param>
        /// <param name="postImageName">If you have a specific post image name, enter it, otherwise to first one will be used</param>
        /// <param name="index">Index is only used for collection of targets, pre/post images</param>
        public ContextEntity(IPluginExecutionContext5 context, string preImageName = null, string postImageName = null, int index = -1)
        {
            this.context = context;
            this.preImageName = preImageName;
            this.postImageName = postImageName;
            Index = index;
        }

        /// <summary>
        /// Get the Entity by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

    public class ContextEntityCollection : IEnumerable<ContextEntity>
    {
        private List<ContextEntity> _entities = new List<ContextEntity>();

        /// <summary>
        /// Contructor of ContextEntityCollection to access a list of Targets, PreImages, PostImages and Complete
        /// </summary>
        /// <param name="context">IPluginExecutionContext5 is all you need to use this class</param>
        /// <param name="preImageName">If you have a specific pre image name, enter it, otherwise to first one will be used</param>
        /// <param name="postImageName">If you have a specific post image name, enter it, otherwise to first one will be used</param>
        public ContextEntityCollection(IPluginExecutionContext5 context, string preImageName = null, string postImageName = null)
        {
            if (context?.InputParameters?.Contains(ParameterName.Targets) == true &&
                context.InputParameters[ParameterName.Targets] is EntityCollection entityCollection)
            {
                var i = 0;
                entityCollection.Entities.ToList().ForEach(_ => _entities.Add(new ContextEntity(context, preImageName, postImageName, i++)));
            }
        }

        public IEnumerator<ContextEntity> GetEnumerator() => _entities.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class ContextEntityExtensions
    {
        /// <summary>
        /// Simple, but smart, Merge extension to Entity
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <returns></returns>
        public static Entity Merge(this Entity entity1, Entity entity2)
        {
            var merge = entity1.Clone(false) ?? entity2.Clone(false);
            if (entity1 != null && entity2 != null)
            {
                merge.Attributes.AddRange(entity2.Attributes.Where(a => !merge.Attributes.Contains(a.Key)));
            }
            return merge;
        }

        /// <summary>
        /// Simple, but smart, Clone
        /// </summary>
        /// <param name="entity">Entity to clone</param>
        /// <param name="onlyId">True if only Id shall be used, otherwise all attributes will be include</param>
        /// <returns></returns>
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

        public static T Clone<T>(this T entity, bool onlyId) where T : Entity => Clone((Entity)entity, onlyId).ToEntity<T>();
    }

    public enum ContextEntityType
    {
        Target,
        PreImage,
        PostImage,
        Complete
    }
}