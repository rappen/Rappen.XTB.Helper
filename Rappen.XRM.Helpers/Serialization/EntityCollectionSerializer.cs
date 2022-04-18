using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace Rappen.XRM.Helpers.Serialization
{
    public class EntityCollectionSerializer
    {
        public static XmlDocument Serialize(EntityCollection collection, SerializationStyle style = SerializationStyle.Basic)
        {
            var result = new XmlDocument();
            XmlNode root = result.CreateNode(XmlNodeType.Element, "Entities", "");
            var entityname = result.CreateAttribute("EntityName");
            entityname.Value = collection.EntityName;
            root.Attributes.Append(entityname);
            var more = result.CreateAttribute("MoreRecords");
            more.Value = collection.MoreRecords.ToString();
            root.Attributes.Append(more);
            var total = result.CreateAttribute("TotalRecordCount");
            total.Value = collection.TotalRecordCount.ToString();
            root.Attributes.Append(total);
            var paging = result.CreateAttribute("PagingCookie");
            paging.Value = collection.PagingCookie;
            root.Attributes.Append(paging);
            foreach (var entity in collection.Entities)
            {
                EntitySerializer.Serialize(entity, root, style);
            }
            result.AppendChild(root);
            return result;
        }

        public static EntityCollection Deserialize(XmlDocument serializedEntities)
        {
            var ec = new EntityCollection();
            if (serializedEntities != null && serializedEntities.ChildNodes.Count > 0)
            {
                if (serializedEntities.ChildNodes[0].Name == "Entities")
                {
                    var entityName = string.Empty;
                    foreach (XmlNode xEntity in serializedEntities.ChildNodes[0].ChildNodes)
                    {
                        var entity = EntitySerializer.Deserialize(xEntity);
                        ec.Entities.Add(entity);
                        if (string.IsNullOrEmpty(entityName))
                        {
                            entityName = entity.LogicalName;
                        }
                        if (!entityName.Equals(entity.LogicalName))
                        {
                            entityName = "[multipleentities]";
                        }
                    }
                    if (!entityName.Equals("[multipleentities]"))
                    {
                        ec.EntityName = entityName;
                    }
                }
                else
                {
                    var serializer = new DataContractSerializer(typeof(EntityCollection), new List<Type> { typeof(Entity) });
                    var sr = new StringReader(serializedEntities.OuterXml);
                    using (var reader = new XmlTextReader(sr))
                    {
                        ec = (EntityCollection)serializer.ReadObject(reader);
                    }
                    sr.Close();
                }
            }
            return ec;
        }

        public static string ToJSONComplex(EntityCollection collection, Formatting format)
        {
            var rootDictionary = new Dictionary<string, object>
            {
                ["entities"] = collection.Entities.Select(e => EntitySerializer.ToJSONComplexObject(e)).ToArray()
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(rootDictionary, format == System.Xml.Formatting.Indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }

        public static string ToJSONSimple(EntityCollection collection, Formatting format)
        {
            var rootDictionary = new Dictionary<string, object>
            {
                ["value"] = collection.Entities.Select(e => EntitySerializer.ToJSONSimpleObject(e)).ToArray()
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(rootDictionary, format == Formatting.Indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }
    }
}