using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Rappen.XRM.Helpers.FetchXML
{
    public abstract class FetchXMLBase
    {
        private string name;
        private List<string> knownAttributes;
        private List<string> knownNodes;
        private Dictionary<string, string> unknowsAttributes;
        private Dictionary<string, XmlElement> unknowsNodes;

        internal Fetch Fetch;

        protected abstract List<string> GetKnownAttributes();
        protected abstract List<string> GetKnownNodes();
        protected abstract void AddXMLProperties(XmlElement xml);

        public bool IncludeUnknown = true;

        protected FetchXMLBase(Fetch fetch, XmlNode xml)
        {
            name = xml.Name;
            IncludeUnknown = fetch?.IncludeUnknown ?? false;
            Fetch = fetch ?? this as Fetch;
            knownAttributes = GetKnownAttributes();
            knownNodes = GetKnownNodes();
            unknowsAttributes = xml.Attributes?.Cast<XmlAttribute>().Where(a => !knownAttributes.Contains(a.Name)).ToDictionary(a => a.Name, a => a.Value);
            unknowsNodes = xml.ChildNodes?.Cast<XmlNode>().Where(n => n is XmlElement && !knownNodes.Contains(n.Name)).ToDictionary(n => n.Name, n => n as XmlElement);
        }

        public override string ToString() => ToXML().OuterXml;

        public XmlNode ToXML()
        {
            var xml = Fetch.Xml.CreateElement(name);
            AddXMLProperties(xml);
            if (IncludeUnknown)
            {
                unknowsAttributes?.ToList().ForEach(u => xml.AddAttribute(u.Key, u.Value));
                // More works is needed...
                //     unknowsNodes?.ToList().ForEach(u => xml.AppendChild(u.Value.CloneNode(true)));
            }
            return xml;
        }
    }

    public class ControlValidationResult
    {
        private const string IsRequired = "{0} is required";
        private const string InValid = "{0} is not valid";
        private const string NotInMetadata = "{0} is not in the database.";
        private const string NotShowingNow = "{0} is not currently shown.";

        public ControlValidationResult(ControlValidationLevel level, string message, string url = null)
        {
            Level = level;
            Message = message;
            Url = url;
        }

        public ControlValidationResult(ControlValidationLevel level, string control, ControlValidationMessage message, string url = null)
        {
            Level = level;
            switch (message)
            {
                case ControlValidationMessage.IsRequired:
                    Message = string.Format(IsRequired, control);
                    break;

                case ControlValidationMessage.InValid:
                    Message = string.Format(InValid, control);
                    break;

                case ControlValidationMessage.NotInMetadata:
                    Message = string.Format(NotInMetadata, control);
                    break;

                case ControlValidationMessage.NotShowingNow:
                    Message = string.Format(NotShowingNow, control);
                    break;
            }
            Url = url;
        }

        public ControlValidationLevel Level { get; }
        public string Message { get; }
        public string Url { get; }
    }

    public enum ControlValidationLevel
    {
        Success,
        Info,
        Warning,
        Error
    }

    public enum ControlValidationMessage
    {
        IsRequired,
        InValid,
        NotInMetadata,
        NotShowingNow
    }
}