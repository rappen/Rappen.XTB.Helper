using Microsoft.Xrm.Sdk;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.XRM.Helpers.Plugin
{
    public class ContextEntityCollection : IEnumerable<ContextEntity>
    {
        private List<ContextEntity> _entities = new List<ContextEntity>();

        public ContextEntityCollection(IPluginExecutionContext5 context, string preImageName = null, string postimagename = null)
        {
            if (context?.InputParameters?.Contains(ParameterName.Targets) == true &&
                context.InputParameters[ParameterName.Targets] is EntityCollection entityCollection)
            {
                var i = 0;
                entityCollection.Entities.ToList().ForEach(_ => _entities.Add(new ContextEntity(context, preImageName, postimagename, i++)));
            }
        }

        public IEnumerator<ContextEntity> GetEnumerator() => _entities.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}