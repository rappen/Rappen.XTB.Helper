using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Interfaces;
using System;

namespace Rappen.XRM.Tokens
{
    internal static class Randoms
    {
        private static Random random;

        internal static string ReplaceRandom(IBag bag, string text, string token)
        {
            bag.Logger.StartSection("ReplaceRandom " + token);
            string type = token.GetSeparatedPart("|", 2);
            string param1 = token.GetSeparatedPart("|", 3);
            string param2 = token.GetSeparatedPart("|", 4);
            bag.Logger.Log($"Random: {type} Params: {param1}, {param2}");

            var value = string.Empty;
            if (random == null)
            {
                random = new Random();
            }
            switch (type.ToUpperInvariant())
            {
                case "TEXT":
                    value = RandomText(param1, param2);
                    break;

                case "NUMBER":
                    value = RandomNumber(param1, param2);
                    break;

                case "DATE":
                    value = RandomDate(param1, param2);
                    break;

                case "GUID":
                    value = Guid.NewGuid().ToString();
                    break;

                default:
                    throw new InvalidPluginExecutionException($"Invalid random type ({type})");
            }
            bag.Logger.Log($"Replacing <{token}> with {value}");
            bag.Logger.EndSection();
            return text.ReplaceFirstOnly("<" + token + ">", value);
        }

        private static string RandomText(string lengthstr, string characters)
        {
            int.TryParse(lengthstr, out var length);
            if (length <= 0)
            {
                throw new InvalidPluginExecutionException("Random length must be a positive integer (" + lengthstr + ")");
            }
            if (string.IsNullOrWhiteSpace(characters))
            {
                characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            }
            var charlength = characters.Length;
            var result = string.Empty;
            for (int i = 0; i < length; i++)
            {
                var randval = random.Next(charlength);
                result += characters[randval];
            }
            return result;
        }

        private static string RandomNumber(string fromstr, string tostr)
        {
            if (!int.TryParse(fromstr, out int from))
            {
                from = int.MaxValue - 1;
            }
            if (!int.TryParse(tostr, out int to))
            {
                to = from;
                from = 0;
            }
            if (from > to)
            {
                throw new InvalidPluginExecutionException($"Number area is incorrect ({fromstr} - {tostr})");
            }
            return random.Next(from, to + 1).ToString();
        }

        private static string RandomDate(string fromstr, string tostr)
        {
            if (!DateTime.TryParse(fromstr, out DateTime from))
            {
                from = DateTime.MinValue;
            }
            if (!DateTime.TryParse(tostr, out DateTime to))
            {
                to = DateTime.MaxValue;
            }
            if (from > to)
            {
                throw new InvalidPluginExecutionException($"Date area is incorrect ({from} - {to})");
            }
            var datearea = (to - from).TotalDays;
            var days = random.Next((int)datearea);
            var randomday = from.AddDays(days);
            return randomday.ToString("d");
        }
    }
}