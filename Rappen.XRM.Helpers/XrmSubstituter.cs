using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Interfaces;
using System;

namespace Rappen.XRM.Helpers
{
    public static class XrmSubstituter
    {
        #region Public extensions methods

        [Obsolete("Use XRM Tokens instead", true)]
        public static string Substitute(this Entity entity, IOrganizationService service, string text) => Substitute(entity, new GenericBag(service), text);

        [Obsolete("Use XRM Tokens instead", true)]
        public static string Substitute(this Entity entity, IBag bag, string text) => Substitute(entity, bag, text, 0, string.Empty);

        [Obsolete("Use XRM Tokens instead", true)]
        public static string Substitute(this Entity entity, IBag bag, string text, int sequence) => Substitute(entity, bag, text, sequence, string.Empty);

        [Obsolete("Use XRM Tokens instead", true)]
        public static string Substitute(this Entity entity, IBag bag, string text, int sequence, string scope) => Substitute(entity, bag, text, sequence, scope, false);

        [Obsolete("Use XRM Tokens instead", true)]
        public static string Substitute(this Entity entity, IBag bag, string text, int sequence, string scope, bool supressinvalidattributepaths) => null;

        #endregion Public extensions methods
    }
}