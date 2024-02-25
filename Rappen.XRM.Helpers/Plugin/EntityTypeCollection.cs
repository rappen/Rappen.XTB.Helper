using Microsoft.Xrm.Sdk;
using System.Collections;
using System.Collections.Generic;

namespace Rappen.XRM.Helpers.Plugin
{
    public class EntityTypeCollection : IEnumerable<EntityType>
    {
        private readonly List<EntityType> entityTypes = new List<EntityType>();

        public EntityTypeCollection(PluginBag bag)
        {
            if (bag?.Context?.InputParameters?.Contains(ParameterName.Targets) == true &&
                bag.Context.InputParameters[ParameterName.Targets] is EntityCollection entityCollection)
            {
                for (var i = 0; i < entityCollection.Entities.Count; i++)
                {
                    entityTypes.Add(new EntityType(bag, i));
                }
            }
        }

        public IEnumerator<EntityType> GetEnumerator() => entityTypes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}