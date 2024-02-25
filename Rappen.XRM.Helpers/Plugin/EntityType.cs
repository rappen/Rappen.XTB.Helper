using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Extensions;
using System.Linq;

namespace Rappen.XRM.Helpers.Plugin
{
    public class EntityType
    {
        private IPluginExecutionContext5 context;

        public EntityType(PluginBag bag, int index = -1)
        {
            context = bag.Context;
            Target = GetTargetEntity(index);
            bag.Logger.Log($"Target {index} {Target.LogicalName} {Target.Id}");
            PreImage = GetPreEntity(index);
            PostImage = GetPostEntity(index);
            Complete = GetCompleteEntity(index);
        }

        public Entity Target;
        public Entity PreImage;
        public Entity PostImage;
        public Entity Complete;

        private Entity GetTargetEntity(int index)
        {
            if (index < 0)
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
                    targets.Entities.Count > index)
                {
                    return targets.Entities[index];
                }
            }
            return null;
        }

        private Entity GetPreEntity(int index)
        {
            if (index < 0)
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
                    context.PreEntityImagesCollection.Length > index)
                {
                    return context.PreEntityImagesCollection[index].FirstOrDefault().Value;
                }
            }

            return null;
        }

        private Entity GetPostEntity(int index)
        {
            if (index < 0)
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
                    context.PostEntityImagesCollection.Length > index)
                {
                    return context.PostEntityImagesCollection[index].FirstOrDefault().Value;
                }
            }

            return null;
        }

        private Entity GetCompleteEntity(int index) => GetTargetEntity(index).Merge(GetPostEntity(index)).Merge(GetPreEntity(index));
    }
}