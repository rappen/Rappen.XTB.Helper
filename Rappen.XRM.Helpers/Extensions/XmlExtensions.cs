﻿using System;
using System.IO;
using System.Xml;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class XmlExtensions
    {
        public static XmlDocument ToXml(this string XmlString, bool IgnoreXmlErrors = true)
        {
            var xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(XmlString);
            }
            catch (XmlException e)
            {
                if (!IgnoreXmlErrors)
                {
                    throw e;
                }
            }
            return xmldoc;
        }

        public static XmlNode ToXmlNode(this string XmlString, bool IgnoreXmlErrors = true)
        {
            var xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(XmlString);
            }
            catch (XmlException e)
            {
                if (!IgnoreXmlErrors)
                {
                    throw e;
                }
            }
            return xmldoc.DocumentElement;
        }

        public static string AttributeValue(this XmlNode node, string key)
        {
            if (node != null && node.Attributes != null && node.Attributes[key] is XmlAttribute attr)
            {
                return attr.Value;
            }
            return string.Empty;
        }

        public static string ToString(this XmlNode xmlNode, bool formatted)
        {
            if (xmlNode == null)
                throw new ArgumentNullException(nameof(xmlNode));

            if (!formatted)
            {
                return xmlNode.OuterXml;
            }
            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
                {
                    xmlTextWriter.Formatting = Formatting.Indented; // Set formatting to indented
                    xmlNode.WriteTo(xmlTextWriter);
                }
                return stringWriter.ToString();
            }
        }
    }
}