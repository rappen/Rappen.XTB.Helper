using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Globalization;

namespace Rappen.XRM.Helpers.Extensions
{
    public static class StringParses
    {
        public static object TryParseTo(this string text, AttributeTypeCode attributetypecode, AttributeMetadata metadata = null)
        {
            var textdecimal = text.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            switch (attributetypecode)
            {
                case AttributeTypeCode.Boolean:
                    if (bool.TryParse(text, out bool boolvalue))
                    {
                        return boolvalue;
                    }
                    if (text == "1" || text == "0")
                    {
                        return text == "1";
                    }
                    throw new Exception("Not valid format [True|False] or [1|0]");
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                    var entity = string.Empty;
                    var id = Guid.Empty;
                    if (Guid.TryParse(text, out id))
                    {
                        var lookupmeta = metadata as LookupAttributeMetadata;
                        if (lookupmeta?.Targets?.Length == 1)
                        {
                            entity = lookupmeta.Targets[0];
                        }
                    }
                    else if (text.Contains(":"))
                    {
                        if (Guid.TryParse(text.Split(':')[1], out id))
                        {
                            entity = text.Split(':')[0];
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(entity) && !id.Equals(Guid.Empty))
                    {
                        return new EntityReference(entity, id);
                    }
                    throw new Exception("Not valid format [Entity:]Guid");
                case AttributeTypeCode.DateTime:
                    if (DateTime.TryParse(text, out DateTime datetime))
                    {
                        return datetime;
                    }
                    throw new Exception("Not valid format Date[Time]");
                case AttributeTypeCode.Decimal:
                    if (decimal.TryParse(textdecimal, out decimal decimalvalue))
                    {
                        return decimalvalue;
                    }
                    break;

                case AttributeTypeCode.Double:
                    if (double.TryParse(textdecimal, out double doublevalue))
                    {
                        return doublevalue;
                    }
                    break;

                case AttributeTypeCode.Integer:
                case AttributeTypeCode.BigInt:
                    if (int.TryParse(text, out int intvalue))
                    {
                        return intvalue;
                    }
                    break;

                case AttributeTypeCode.Money:
                    if (decimal.TryParse(textdecimal, out decimal moneyvalue))
                    {
                        return new Money(moneyvalue);
                    }
                    break;

                case AttributeTypeCode.Picklist:
                    if (int.TryParse(text, out int pickvalue))
                    {
                        return new OptionSetValue(pickvalue);
                    }
                    break;

                case AttributeTypeCode.Virtual:
                    if (metadata is MultiSelectPicklistAttributeMetadata multi)
                    {
                        var result = new OptionSetValueCollection();
                        foreach (var optionvalue in text.Split(';'))
                        {
                            if (int.TryParse(optionvalue, out int value))
                            {
                                result.Add(new OptionSetValue(value));
                            }
                        }
                        return result;
                    }
                    else
                    {
                        throw new Exception($"Not supporting {metadata?.ToString().Replace("Type", "")}");
                    }

                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return text;

                case AttributeTypeCode.Uniqueidentifier:
                    if (Guid.TryParse(text, out Guid guidvalue))
                    {
                        return guidvalue;
                    }
                    break;

                case AttributeTypeCode.Owner:
                case AttributeTypeCode.PartyList:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.CalendarRules:
                case AttributeTypeCode.EntityName:
                case AttributeTypeCode.ManagedProperty:
                    throw new Exception($"Not supporting {attributetypecode}");
            }
            throw new Exception($"Not valid {attributetypecode}");
        }
    }
}