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
    }
}
