// *********************************************************************
// Created by : Latebound Constants Generator 1.2024.11.4 for XrmToolBox
// Tool Author: Jonas Rapp https://jonasr.app/
// GitHub     : https://github.com/rappen/LCG-UDG/
// Source Org : https://jonasspace.crm4.dynamics.com/
// Filename   : C:\Dev\AzDO-JR\Rapp Tools\Development\Shared\Rappen.XTB.Helper\Rappen.XRM.Helpers\RappSack\EnvironmentVariablesConsts.cs
// Created    : 2025-02-22 11:26:10
// *********************************************************************

namespace Rappen.XRM.RappSack
{
    /// <summary>DisplayName: Environment Variable Definition, OwnershipType: UserOwned, IntroducedVersion: 1.0.0.0</summary>
    /// <remarks>Contains information about the settable variable: its type, default value, and etc.</remarks>
    public static class EnvironmentVariableDefinition
    {
        public const string EntityName = "environmentvariabledefinition";
        public const string EntityCollectionName = "environmentvariabledefinitions";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        /// <remarks>Unique identifier for entity instances</remarks>
        public const string PrimaryKey = "environmentvariabledefinitionid";
        /// <summary>Type: String, RequiredLevel: SystemRequired, MaxLength: 100, Format: Text</summary>
        /// <remarks>Unique entity name.</remarks>
        public const string PrimaryName = "schemaname";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        /// <remarks>Default variable value to be used if no associated EnvironmentVariableValue entities exist.</remarks>
        public const string DefaultValue = "defaultvalue";
        /// <summary>Type: String, RequiredLevel: ApplicationRequired, MaxLength: 100, Format: Text</summary>
        /// <remarks>Display Name of the variable definition.</remarks>
        public const string DisplayName = "displayname";
        /// <summary>Type: Picklist, RequiredLevel: ApplicationRequired, DisplayName: Type, OptionSetType: Picklist, DefaultFormValue: 100000000</summary>
        /// <remarks>Environment variable value type.</remarks>
        public const string Type = "type";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        /// <remarks>For internal use only.</remarks>
        public const string ValueSchema = "valueschema";

        #endregion Attributes

        #region Relationships

        /// <summary>Parent: "EnvironmentVariableDefinition" Child: "EnvironmentVariableDefinition" Lookup: "ParentDefinitionId"</summary>
        public const string Rel1M_envdefinition_envdefinition = "envdefinition_envdefinition";
        /// <summary>Parent: "EnvironmentVariableDefinition" Child: "EnvironmentVariableValue" Lookup: "EnvironmentVariableDefinitionId"</summary>
        public const string Rel1M_environmentvariabledefinition_environmentvariablevalue = "environmentvariabledefinition_environmentvariablevalue";

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

    /// <summary>DisplayName: Environment Variable Value, OwnershipType: None, IntroducedVersion: 1.0.0.0</summary>
    /// <remarks>Holds the value for the associated EnvironmentVariableDefinition entity.</remarks>
    public static class EnvironmentVariableValue
    {
        public const string EntityName = "environmentvariablevalue";
        public const string EntityCollectionName = "environmentvariablevalues";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        /// <remarks>Unique identifier for entity instances</remarks>
        public const string PrimaryKey = "environmentvariablevalueid";
        /// <summary>Type: String, RequiredLevel: ApplicationRequired, MaxLength: 100, Format: Text</summary>
        /// <remarks>Unique entity name.</remarks>
        public const string PrimaryName = "schemaname";
        /// <summary>Type: Lookup, RequiredLevel: SystemRequired, Targets: environmentvariabledefinition</summary>
        /// <remarks>Unique identifier for Environment Variable Definition associated with Environment Variable Value.</remarks>
        public const string EnvironmentVariableDefinitionId = "environmentvariabledefinitionid";
        /// <summary>Type: Memo, RequiredLevel: None, MaxLength: 2000</summary>
        /// <remarks>Contains the actual variable data.</remarks>
        public const string Value = "value";

        #endregion Attributes
    }
}

/***** LCG-configuration-BEGIN ****
<?xml version="1.0" encoding="utf-16"?>
<Settings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <TemplateFormat>Constants</TemplateFormat>
  <Version>1.2024.11.4</Version>
  <NameSpace>Rappen.XRM.Helpers.RappSack</NameSpace>
  <ColorByType>true</ColorByType>
  <UseCommonFile>true</UseCommonFile>
  <SaveConfigurationInCommonFile>true</SaveConfigurationInCommonFile>
  <FileName>DisplayName</FileName>
  <ConstantName>SchemaName</ConstantName>
  <ConstantCamelCased>false</ConstantCamelCased>
  <DoStripPrefix>false</DoStripPrefix>
  <StripPrefix>_</StripPrefix>
  <XmlProperties>true</XmlProperties>
  <XmlDescription>true</XmlDescription>
  <TypeDetails>false</TypeDetails>
  <Regions>true</Regions>
  <RelationShips>true</RelationShips>
  <RelationshipLabels>false</RelationshipLabels>
  <OptionSets>true</OptionSets>
  <GlobalOptionSets>false</GlobalOptionSets>
  <Legend>false</Legend>
  <TableSize>1</TableSize>
  <RelationShipSize>2</RelationShipSize>
  <CommonAttributes>None</CommonAttributes>
  <AttributeSortMode>None</AttributeSortMode>
  <Groups />
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
**** LCG-configuration-END   *****/