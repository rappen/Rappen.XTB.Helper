using Microsoft.Xrm.Sdk;
using System;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class EntityCollectionExtensions
    {
        public static void Remove(this EntityCollection entities, Guid id)
        {
            var i = 0;
            while (i < entities.Entities.Count)
            {
                if (entities.Entities[i].Id.Equals(id))
                {
                    entities.Entities.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
    }
}