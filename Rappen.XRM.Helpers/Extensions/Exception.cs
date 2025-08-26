using System;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class Exceptions
    {
        public static string ToTypeString(this System.Exception ex)
        {
            var type = ex.GetType().ToString();
            if (type.Contains("`1["))
            {
                type = type.Replace("`1[", "<").Replace("]", ">");
            }
            return type;
        }

        public static string ExceptionDetails(this Exception ex, int level = 0) => ex == null ? string.Empty :
              $"{new string(' ', level * 2)}{ex.Message}{Environment.NewLine}{ex.InnerException.ExceptionDetails(level + 1)}".Trim();
    }
}