using System;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public static class XmlExtensions
    {
        public static string Attribute(this XmlNode node, string name)
        {
            if (node != null && node.Attributes != null && node.Attributes[name] != null && !string.IsNullOrWhiteSpace(node.Attributes[name].Value))
            {
                return node.Attributes[name].Value;
            }
            return null;
        }

        public static bool? AttributeBool(this XmlNode node, string name)
        {
            var value = node.Attribute(name);
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return null;
        }

        public static int? AttributeInt(this XmlNode node, string name)
        {
            var value = node.Attribute(name);
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return null;
        }

        public static T ToEnum<T>(this string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    return (T)Enum.Parse(typeof(T), value);
                }
                catch { }
            }
            return default(T);
        }
    }
}
