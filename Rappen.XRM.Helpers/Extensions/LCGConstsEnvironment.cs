// *********************************************************************
// Created by : Latebound Constants Generator 1.2021.12.1 for XrmToolBox
// Author     : Jonas Rapp https://jonasr.app/
// GitHub     : https://github.com/rappen/LCG-UDG/
// Source Org : https://jonasspace.crm4.dynamics.com
// Filename   : C:\Dev\GitHub\Rappen.DV.Namings\Rappen.XTB.Helper\Extensions\LCGConstsEnvironment.cs
// Created    : 2022-01-22 13:47:51
// *********************************************************************

namespace Rappen.XRM.Helpers.Extensions
{
    /// <summary>DisplayName: Environment Variable Definition, OwnershipType: UserOwned, IntroducedVersion: 1.0.0.0</summary>
    public static class Environmentvariabledefinition
    {
        public const string EntityName = "environmentvariabledefinition";
        public const string EntityCollectionName = "environmentvariabledefinitions";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        public const string PrimaryKey = "environmentvariabledefinitionid";
        /// <summary>Type: String, RequiredLevel: SystemRequired, MaxLength: 100, Format: Text</summary>
        public const string PrimaryName = "schemaname";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        public const string Defaultvalue = "defaultvalue";
        /// <summary>Type: String, RequiredLevel: ApplicationRequired, MaxLength: 100, Format: Text</summary>
        public const string DisplayName = "displayname";
        /// <summary>Type: Picklist, RequiredLevel: ApplicationRequired, DisplayName: Type, OptionSetType: Picklist, DefaultFormValue: 100000000</summary>
        public const string Type = "type";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        public const string Valueschema = "valueschema";

        #endregion Attributes

        #region Relationships

        /// <summary>Parent: "Environmentvariabledefinition" Child: "Environmentvariabledefinition" Lookup: "ParentDefinitionId"</summary>
        public const string Rel1M_EnvdefinitionEnvdefinition = "envdefinition_envdefinition";
        /// <summary>Parent: "Environmentvariabledefinition" Child: "Environmentvariablevalue" Lookup: "EnvironmentvariabledefinitionId"</summary>
        public const string Rel1M_EnvironmentvariabledefinitionEnvironmentvariablevalue = "environmentvariabledefinition_environmentvariablevalue";

        #endregion Relationships

        #region OptionSets

        public enum Type_OptionSet
        {
            String = 100000000,
            Number = 100000001,
            Boolean = 100000002,
            JSON = 100000003,
            DataSource = 100000004,
            Secret = 100000005
        }

        #endregion OptionSets
    }

    /// <summary>DisplayName: Environment Variable Value, OwnershipType: UserOwned, IntroducedVersion: 1.0.0.0</summary>
    public static class Environmentvariablevalue
    {
        public const string EntityName = "environmentvariablevalue";
        public const string EntityCollectionName = "environmentvariablevalues";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        public const string PrimaryKey = "environmentvariablevalueid";
        /// <summary>Type: String, RequiredLevel: ApplicationRequired, MaxLength: 100, Format: Text</summary>
        public const string PrimaryName = "schemaname";
        /// <summary>Type: Lookup, RequiredLevel: ApplicationRequired, Targets: environmentvariabledefinition</summary>
        public const string EnvironmentvariabledefinitionId = "environmentvariabledefinitionid";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        public const string Value = "value";

        #endregion Attributes

        #region Relationships

        /// <summary>Parent: "Environmentvariabledefinition" Child: "Environmentvariablevalue" Lookup: "EnvironmentvariabledefinitionId"</summary>
        public const string RelM1_EnvironmentvariabledefinitionEnvironmentvariablevalue = "environmentvariabledefinition_environmentvariablevalue";

        #endregion Relationships
    }
}


/***** LCG-configuration-BEGIN *****\
<?xml version="1.0" encoding="utf-16"?>
<Settings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Version>1.2021.1.2</Version>
  <NameSpace>Rappen.XRM.Helpers.Extensions</NameSpace>
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
      <Name>environmentvariabledefinition</Name>
      <Attributes>
        <string>defaultvalue</string>
        <string>displayname</string>
        <string>environmentvariabledefinitionid</string>
        <string>schemaname</string>
        <string>type</string>
        <string>valueschema</string>
      </Attributes>
      <Relationships>
        <string>envdefinition_envdefinition</string>
        <string>environmentvariabledefinition_environmentvariablevalue</string>
      </Relationships>
    </SelectedEntity>
    <SelectedEntity>
      <Name>environmentvariablevalue</Name>
      <Attributes>
        <string>environmentvariabledefinitionid</string>
        <string>environmentvariablevalueid</string>
        <string>schemaname</string>
        <string>value</string>
      </Attributes>
      <Relationships>
        <string>environmentvariabledefinition_environmentvariablevalue</string>
      </Relationships>
    </SelectedEntity>
  </SelectedEntities>
</Settings>
\***** LCG-configuration-END   *****/
