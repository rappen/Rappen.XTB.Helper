# Rappen.XTB.Helper

This is a C# Shared Project with various UI controls, helper classes and extention methods to work smoothly with [Microsoft Dataverse SDK](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/?WT.mc_id=BA-MVP-5002475).

---

# Controls

*Custom properties and events are found under category **Rappen XRM**.*

---

## XRMDataComboBox
Just like any ComboBox, but accepts [`EntityCollection`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) or `IEnumerable<Entity>` as `DataSource`.
### Properties
* **`DataSource`**
* **`Service`** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **Display Format** set to any [**XRM Token**](https://jonasr.app/xrm-tokens/) to define how each record is presented.
* **`SelectedEntity`** read this to get the record currently selected.

---

## XRMDataTextBox
Just like any TextBox, but has two new properties [`Entity`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) and [`EntityReference`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entityreference). Both these can be set to display text about the record.
### Properties
* **`Service`** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **Display Format** set to any [**XRM Token**](https://jonasr.app/xrm-tokens/) to define how each record is presented.
* **Record Clickable** defines UX behavior of the textbox when hovering with the mouse.
* **Record Id**
* **Record LogicalName**
### Events
* **RecordClick**

---

## XRMDataGridView
Just like any DataGridView, but accepts [`EntityCollection`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) or `IEnumerable<Entity>` as `DataSource`.
### Properties
* **`Service`** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **`DataSource`**
* **AutoRefresh**
* **ColumnOrder**
* **EntityReferenceClickable**
* **FilterColumns**
* **FilterText**
* **ShowAllColumnsInColumnOrder**
* **ShowColumnsNotInColumnOrder**
* **ShowFriendlyNames**
* **ShowIdColumn**
* **ShowIndexColumn**
* **ShowLocalTimes**
* **`EntityName`**
* **`SelectedRowRecords`**
* **`SelectedCellRecords`**

### Events
* **RecordClick**
* **RecordDoubleClick**
* **RecordEnter**
* **RecordLeave**
* **RecordMouseEnter**
* **RecordMouseLeave**


---

## XRMLookupDialog
Just like a `FileOpenDialog` or `ColorDialog`, but lets the user select an entity record.
### Properties
* **`Service`** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **`Entity`** The selected entity record
* **`Entities`** The selected entity records, when multiselect is allowed
* **LogicalName** Logical name of the entity to select from
* **LogicalNames** Logical names of the entity to select from, like a polymorphic lookup
* **MultiSelect** Set true to allow user to select multiple records
* **ShowFriendlyNames**
* **IncludePersonalViews**
* **Title** of the dialog window

---

## XRMEntityComboBox
Just like any ComboBox, but accepts [`EntityMetadataCollection`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadatacollection?WT.mc_id=BA-MVP-5002475) or `IEnumerable<EntityMetadata>` as `DataSource`.
### Properties
* **Show Friendly Names** determines if Display Name or Logical Name of the entities shall be shown.
* `**SelectedEntity**` read this to get the entity currently selected.

---

## XRMAttributeComboBox
Just like any ComboBox, but accepts [`EntityMetadata`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata?WT.mc_id=BA-MVP-5002475) or `IEnumerable<AttributeMetadata>` as `DataSource`.
### Properties
* **Show Friendly Names** determines if Display Name or Logical Name of the entities shall be shown.
* `**SelectedAttribute**` read this to get the entity currently selected.

---

## XRMOptionSetComboBox
Just like any ComboBox, but accepts [`OptionSetMetadata`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.optionsetmetadata?WT.mc_id=BA-MVP-5002475) or `IEnumerable<OptionMetadata>` as `DataSource`.
### Properties
* **ShowValue** determines if the option value should be shown after the option label.
* `**SelectedOption**` read this to get the entity currently selected.

---

# XrmSubstituter
This is an extension to [`Entity`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) used to replace "XRM Tokens" in a text with dynamic data from Dataverse.

Read more at https://jonasr.app/xrm-tokens

---

# History

This project stems from tools and snippets collected over a number of years.
Some of the controls have been inherited/migrated from the [xrmtb.XrmToolBox.Controls](https://github.com/jamesnovak/xrmtb.XrmToolBox.Controls) project created and maintained by [@jamesnovak](https://github.com/jamesnovak/).
