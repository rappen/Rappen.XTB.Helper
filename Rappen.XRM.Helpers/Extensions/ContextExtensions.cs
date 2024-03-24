using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Plugin;
using System;
using System.Linq;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class ContextExtensions
    {
        #region Public Static Extension Methods

        /// <summary>
        /// Retrieves EntityId from the Context
        /// Create, Update, Delete, SetState, Assign, DeliverIncoming
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Guid EntityId(this IPluginExecutionContext context)
        {
            switch (Enum.Parse(typeof(MessageName), context.MessageName, true))
            {
                case MessageName.Create:
                    if (context.Stage == (int)ProcessingStage.PreValidation ||
                        context.Stage == (int)ProcessingStage.PreOperation)
                    {
                        return Guid.Empty;
                    }
                    else
                    {
                        if (context.OutputParameters.Contains(ParameterName.Id))
                        {
                            return (Guid)context.OutputParameters[ParameterName.Id];
                        }
                    }

                    break;

                case MessageName.DeliverIncoming:
                    if (context.Stage == (int)ProcessingStage.PreValidation ||
                        context.Stage == (int)ProcessingStage.PostOperation)
                    {
                        return Guid.Empty;
                    }
                    else
                    {
                        if (context.OutputParameters.Contains(ParameterName.EmailId))
                        {
                            return (Guid)context.OutputParameters[ParameterName.EmailId];
                        }
                    }

                    break;

                case MessageName.Update:
                case MessageName.Reschedule:
                    if (context.InputParameters[ParameterName.Target] is Entity)
                    {
                        return ((Entity)context.InputParameters[ParameterName.Target]).Id;
                    }

                    break;

                case MessageName.Delete:
                case MessageName.Assign:
                case MessageName.GrantAccess:
                    if (context.InputParameters[ParameterName.Target] is EntityReference)
                    {
                        return ((EntityReference)context.InputParameters[ParameterName.Target]).Id;
                    }

                    break;

                case MessageName.SetState:
                    return ((EntityReference)context.InputParameters[ParameterName.EntityMoniker]).Id;

                default:
                    if (context.InputParameters.Contains(ParameterName.Target) &&
                        (context.InputParameters[ParameterName.Target] is EntityReference))
                    {
                        return ((EntityReference)context.InputParameters[ParameterName.Target]).Id;
                    }

                    //Try by best route else fail
                    return Guid.Empty;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Returnerar true om något attribut i listan återfinns i Target i Context.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public static bool Any(this IPluginExecutionContext context, params string[] attributes)
        {
            if (context.GetInputParameter<Entity>(ParameterName.Target) is Entity target)
            {
                return target.Attributes.Keys.Intersect(attributes).Any();
            }
            return false;
        }

        public static bool Changes(this IPluginExecutionContext context, params string[] attributes)
        {
            if (context.GetInputParameter<Entity>(ParameterName.Target) is Entity target &&
                context.PreEntityImages.Select(p => p.Value).FirstOrDefault() is Entity preimage)
            {
                var postimage = context.PostEntityImages.Select(p => p.Value).FirstOrDefault();
                foreach (var attribute in attributes)
                {
                    var current = postimage.AttributeToString(attribute) ?? target.AttributeToString(attribute);
                    var pre = preimage.AttributeToString(attribute);
                    if (current != pre)
                    {
                        return true;
                    }
                }
                return false;
            }
            return context.Any(attributes);
        }

        public static T GetInputParameter<T>(this IPluginExecutionContext context, string name, string outputerrorparameter = null)
        {
            if (context.InputParameters.Contains(name) && context.InputParameters[name] is T result)
            {
                return result;
            }
            if (!string.IsNullOrEmpty(outputerrorparameter))
            {
                context.OutputParameters[outputerrorparameter] = $"Missing InputParameter {name}.";
            }
            return default;
        }

        #endregion Public Static Extension Methods
    }
}