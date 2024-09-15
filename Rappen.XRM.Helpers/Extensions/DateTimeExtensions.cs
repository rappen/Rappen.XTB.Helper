using System;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class DateTimeExtensions
    {
        public static string MillisecondToSmartString(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds).ToSmartString();

        public static string ToSmartString(this TimeSpan span) => span.ToSmartStringSplit().Item1 + " " + span.ToSmartStringSplit().Item2;

        /// <summary>
        /// Returns smartest string representation of a TimeSpan, separated time and unit
        /// </summary>
        /// <param name="span"></param>
        /// <returns>Item1: Span, Item2: Unit</returns>
        public static Tuple<string, string> ToSmartStringSplit(this TimeSpan span)
        {
            if (span.TotalDays >= 1)
            {
                return new Tuple<string, string>($"{span.TotalDays:0} {span.Hours:00}:{span.Minutes:00}", "days");
            }
            if (span.TotalHours >= 1)
            {
                return new Tuple<string, string>($"{span.Hours:0}:{span.Minutes:00}", "hrs");
            }
            if (span.TotalMinutes >= 1)
            {
                return new Tuple<string, string>($"{span.Minutes:0}:{span.Seconds:00}", "mins");
            }
            if (span.TotalSeconds >= 1)
            {
                return new Tuple<string, string>($"{span.Seconds:0}.{span:fff}", "secs");
            }
            return new Tuple<string, string>($"{span.TotalMilliseconds:0}", "ms");
        }
    }
}