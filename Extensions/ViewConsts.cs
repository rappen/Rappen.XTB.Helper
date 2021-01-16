// *********************************************************************
// Created by : Latebound Constants Generator 1.2021.1.2 for XrmToolBox
// Author     : Jonas Rapp https://jonasr.app/
// GitHub     : https://github.com/rappen/LCG-UDG/
// Source Org : https://jonassandbox.crm4.dynamics.com/
// Filename   : C:\Dev\Lab\Rappen.XTB.Helper.Tester\Rappen.XTB.Helper\Extensions\ViewConsts.cs
// Created    : 2021-01-16 19:30:12
// *********************************************************************

namespace Rappen.XTB.Helpers.Extensions
{
    /// <summary>DisplayName: Saved View, OwnershipType: UserOwned, IntroducedVersion: 5.0.0.0</summary>
    public static class UserQuery
    {
        public const string EntityName = "userquery";
        public const string EntityCollectionName = "userqueries";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        public const string PrimaryKey = "userqueryid";
        /// <summary>Type: String, RequiredLevel: SystemRequired, MaxLength: 200, Format: Text</summary>
        public const string PrimaryName = "name";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 1073741823</summary>
        public const string Columnsetxml = "columnsetxml";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        public const string Description = "description";
        /// <summary>Type: Memo, RequiredLevel: SystemRequired, MaxLength: 1073741823</summary>
        public const string Fetchxml = "fetchxml";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 1073741823</summary>
        public const string Layoutxml = "layoutxml";
        /// <summary>Type: Integer, RequiredLevel: SystemRequired, MinValue: 0, MaxValue: 1000000000</summary>
        public const string QueryType = "querytype";
        /// <summary>Type: EntityName, RequiredLevel: SystemRequired</summary>
        public const string ReturnedTypeCode = "returnedtypecode";
        /// <summary>Type: State, RequiredLevel: SystemRequired, DisplayName: Status, OptionSetType: State</summary>
        public const string StateCode = "statecode";
        /// <summary>Type: Status, RequiredLevel: None, DisplayName: Status Reason, OptionSetType: Status</summary>
        public const string StatusCode = "statuscode";

        #endregion Attributes

        #region OptionSets

        public enum ReturnedTypeCode_OptionSet
        {
        }
        public enum StateCode_OptionSet
        {
            Active = 0,
            Inactive = 1
        }
        public enum StatusCode_OptionSet
        {
            Active = 1,
            All = 3,
            Inactive = 2
        }

        #endregion OptionSets
    }

    /// <summary>DisplayName: View, OwnershipType: OrganizationOwned, IntroducedVersion: 5.0.0.0</summary>
    public static class Savedquery
    {
        public const string EntityName = "savedquery";
        public const string EntityCollectionName = "savedqueries";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        public const string PrimaryKey = "savedqueryid";
        /// <summary>Type: String, RequiredLevel: SystemRequired, MaxLength: 200, Format: Text</summary>
        public const string PrimaryName = "name";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 1073741823</summary>
        public const string Columnsetxml = "columnsetxml";
        /// <summary>Type: Boolean, RequiredLevel: SystemRequired, True: 1, False: 0, DefaultValue: False</summary>
        public const string Isdefault = "isdefault";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        public const string Description = "description";
        /// <summary>Type: EntityName, RequiredLevel: SystemRequired</summary>
        public const string ReturnedTypeCode = "returnedtypecode";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 1073741823</summary>
        public const string Fetchxml = "fetchxml";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 1073741823</summary>
        public const string Layoutxml = "layoutxml";
        /// <summary>Type: Integer, RequiredLevel: SystemRequired, MinValue: 0, MaxValue: 1000000000</summary>
        public const string QueryType = "querytype";
        /// <summary>Type: Boolean, RequiredLevel: SystemRequired, True: 1, False: 0, DefaultValue: False</summary>
        public const string Isquickfindquery = "isquickfindquery";
        /// <summary>Type: Boolean, RequiredLevel: SystemRequired, True: 1, False: 0, DefaultValue: False</summary>
        public const string Ismanaged = "ismanaged";
        /// <summary>Type: State, RequiredLevel: SystemRequired, DisplayName: Status, OptionSetType: State</summary>
        public const string StateCode = "statecode";
        /// <summary>Type: Status, RequiredLevel: None, DisplayName: Status Reason, OptionSetType: Status</summary>
        public const string StatusCode = "statuscode";

        #endregion Attributes

        #region OptionSets

        public enum ReturnedTypeCode_OptionSet
        {
        }
        public enum StateCode_OptionSet
        {
            Active = 0,
            Inactive = 1
        }
        public enum StatusCode_OptionSet
        {
            Active = 1,
            Inactive = 2
        }

        #endregion OptionSets
    }
}


/***** LCG-configuration-BEGIN *****\
<?xml version="1.0" encoding="utf-16"?>
<Settings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Version>1.2021.1.2</Version>
  <NameSpace>Rappen.XTB.Helpers.Extensions</NameSpace>
  <UseCommonFile>true</UseCommonFile>
  <SaveConfigurationInCommonFile>true</SaveConfigurationInCommonFile>
  <FileName>DisplayName</FileName>
  <ConstantName>LogicalName</ConstantName>
  <ConstantCamelCased>true</ConstantCamelCased>
  <DoStripPrefix>false</DoStripPrefix>
  <StripPrefix>_</StripPrefix>
  <XmlProperties>true</XmlProperties>
  <XmlDescription>false</XmlDescription>
  <Regions>true</Regions>
  <RelationShips>true</RelationShips>
  <RelationshipLabels>false</RelationshipLabels>
  <OptionSets>true</OptionSets>
  <GlobalOptionSets>false</GlobalOptionSets>
  <Legend>false</Legend>
  <CommonAttributes>None</CommonAttributes>
  <AttributeSortMode>None</AttributeSortMode>
  <SelectedEntities>
    <SelectedEntity>
      <Name>userquery</Name>
      <Attributes>
        <string>columnsetxml</string>
        <string>description</string>
        <string>fetchxml</string>
        <string>layoutxml</string>
        <string>name</string>
        <string>querytype</string>
        <string>returnedtypecode</string>
        <string>statecode</string>
        <string>statuscode</string>
        <string>userqueryid</string>
      </Attributes>
      <Relationships />
    </SelectedEntity>
    <SelectedEntity>
      <Name>savedquery</Name>
      <Attributes>
        <string>columnsetxml</string>
        <string>isdefault</string>
        <string>description</string>
        <string>returnedtypecode</string>
        <string>fetchxml</string>
        <string>layoutxml</string>
        <string>name</string>
        <string>querytype</string>
        <string>isquickfindquery</string>
        <string>ismanaged</string>
        <string>statecode</string>
        <string>statuscode</string>
        <string>savedqueryid</string>
      </Attributes>
      <Relationships />
    </SelectedEntity>
  </SelectedEntities>
</Settings>
\***** LCG-configuration-END   *****/
