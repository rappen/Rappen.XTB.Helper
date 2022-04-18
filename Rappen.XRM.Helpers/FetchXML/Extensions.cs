using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

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

        public static void AddAttribute(this XmlElement xml, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                xml.SetAttribute(name, value.ToString());
            }
        }

        public static void AddAttribute(this XmlElement xml, string name, int? value)
        {
            if (value != null)
            {
                xml.SetAttribute(name, value.ToString());
            }
        }

        public static void AddAttribute(this XmlElement xml, string name, bool? value)
        {
            if (value != null)
            {
                xml.SetAttribute(name, value.ToString().ToLowerInvariant());
            }
        }

        public static string EnumToString<T>(this T pEnumVal)
        {
            // http://stackoverflow.com/q/3047125/194717
            var type = pEnumVal.GetType();
            var info = type.GetField(Enum.GetName(typeof(T), pEnumVal));
            var xmlattributes = info.GetCustomAttributes(typeof(XmlEnumAttribute), false);
            var att = (XmlEnumAttribute)xmlattributes.FirstOrDefault(xa => xa is XmlEnumAttribute);
            if (att != null)
            {
                return att.Name;
            }
            return pEnumVal.ToString();
        }

        public static T StringToEnum<T>(this string value)
        {
            // http://stackoverflow.com/a/3073272/194717
            foreach (object o in System.Enum.GetValues(typeof(T)))
            {
                T enumValue = (T)o;
                if (EnumToString<T>(enumValue).Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)o;
                }
            }

            throw new ArgumentException("No XmlEnumAttribute code exists for type " + typeof(T).ToString() + " corresponding to value of " + value);
        }
    }
}
