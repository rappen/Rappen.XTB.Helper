# Rappen.XTB.Helper

This is a C# Shared Project with various [WinForm](https://docs.microsoft.com/en-us/visualstudio/ide/create-csharp-winform-visual-studio?WT.mc_id=BA-MVP-5002475) UI controls, helper classes and extention methods to work smoothly with [Microsoft Dataverse SDK](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/?WT.mc_id=BA-MVP-5002475).

## Adding to your project

Just open a terminal at the root of a local repo and type:
```
git submodule add https://github.com/rappen/Rappen.XTB.Helper
```

Then right click your solution in Visual Studio, select Add Existing Project, and include the `Rappen.XTB.Helpers.shproj` project from the cloned submodule.

In your project, add a Reference to the added shared project `Rappen.XTB.Helpers`.

Tadaa! 🎉

Helper and extension classes can now be used and examined.

Open a WinForm designer, and all the controls listed below should be available under a group with the name of your project.

---

# Controls

*Custom properties and events are found under category **Rappen XRM**.*

---

## XRMRecordTextBox
Just like any TextBox, but shows values from a record in Dataverse. The control has two new properties [`Entity`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) and [`EntityReference`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entityreference). Both these can be set to display text about the record.
### Properties
* **`Service`** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **Display Format** set to any [**XRM Token**](https://jonasr.app/xrm-tokens/) to define how each record is presented.
* **Record Clickable** defines UX behavior of the textbox when hovering with the mouse.
* **Record LogicalName**
* **Record Id**
### Events
* **RecordClick**

---

## XRMRecordCheckBox
Just like any CheckBox, but shows values from a bool / two option / yesno attribute on a record in Dataverse. The control has two new properties [`Entity`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) and [`EntityReference`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entityreference). Both these can be set to display checked state about an attribute on the record. In addition to that, the new property `Attribute` must be set, specifying which attribute to bind the control to.
### Properties
* **`Service`** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **Attribute** set to any Yes/No column.
* **Record LogicalName**
* **Record Id**

---

## XRMDataComboBox
Just like any ComboBox, but shows data (records) from Dataverse. The control accepts [`EntityCollection`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) or `IEnumerable<Entity>` as `DataSource`.
### Properties
* **`DataSource`**
* **`Service`** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **Display Format** set to any [**XRM Token**](https://jonasr.app/xrm-tokens/) to define how each record is presented.
* **`SelectedEntity`** read this to get the record currently selected.

---

## XRMDataGridView
Just like any DataGridView, but shows data (records) from Dataverse. The control accepts [`EntityCollection`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) or `IEnumerable<Entity>` as `DataSource`.
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
This was partially in turn inherited/migrated from [CRMWinForm](https://github.com/rappen/CRMWinForm) by [@rappen](https://github.com/rappen/).
