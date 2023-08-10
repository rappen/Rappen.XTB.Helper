using System.Data;

namespace Rappen.XTB.Helpers.Extensions
{
    internal static class DataGridExtensions
    {
        private const string _extendedFriendlyName = "FriendlyName";

        internal static bool GetFriendly(this DataColumn column)
        {
            if (column != null && column.ExtendedProperties?.ContainsKey(_extendedFriendlyName) == true)
            {
                return (bool)column.ExtendedProperties[_extendedFriendlyName];
            }
            return false;
        }
        internal static void SetFriendly(this DataColumn column, bool friendly)
        {
            if (column == null)
            {
                return;
            }
            if (column.ExtendedProperties?.ContainsKey(_extendedFriendlyName) == true)
            {
                column.ExtendedProperties[_extendedFriendlyName] = friendly;
            }
            else
            {
                column.ExtendedProperties[_extendedFriendlyName] = friendly;
            }
        }
    }
}