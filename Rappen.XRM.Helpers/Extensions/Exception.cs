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

        public static string MillisecondToSmartString(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds).ToSmartString();

        public static string ToSmartString(this TimeSpan span)
        {
            if (span.TotalDays >= 1)
            {
                return $"{span.TotalDays:0} {span.Hours:00}:{span.Minutes:00} days";
            }
            if (span.TotalHours >= 1)
            {
                return $"{span.Hours:0}:{span.Minutes:00} hrs";
            }
            if (span.TotalMinutes >= 1)
            {
                return $"{span.Minutes:0}:{span.Seconds:00} mins";
            }
            if (span.TotalSeconds >= 1)
            {
                return $"{span.Seconds:0}.{span:fff} secs";
            }
            return $"{span.TotalMilliseconds:0} ms";
        }
    }
}