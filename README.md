# Rappen.XTB.Helper

This is a C# Shared Project with various [WinForm](https://docs.microsoft.com/en-us/visualstudio/ide/create-csharp-winform-visual-studio?WT.mc_id=BA-MVP-5002475) UI controls, helper classes and extention methods to work smoothly with [Microsoft Dataverse SDK](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/?WT.mc_id=BA-MVP-5002475).

## Adding to your project

1. Open a terminal at the root of a local repo and type: `git submodule add https://github.com/rappen/Rappen.XTB.Helper`
1. Right click your solution in Visual Studio, select Add Existing Project, and include the `Rappen.XTB.Helpers.shproj` project from the cloned submodule.
1. In your project, add a Reference to the added shared project `Rappen.XTB.Helpers`.
1. Compile the project once, to make sure everything works and to make it possible for Visual Studio to find the controls in the tool window.
1. Open a WinForm designer and the Tool window, and all the controls listed below should be available under a group with the name of your project.
1. Helper and extension classes can now be used and examined within the added project.

&nbsp;
## Sample Code

Check out repo [Rappen.XTB.Helper.Tester](https://github.com/rappen/Rappen.XTB.Helper.Tester) for a complete sample project using all controls.

---
&nbsp;

# Controls

*Custom published properties and events are found under category **Rappen XRM**.*

---
&nbsp;
## XRMRecordHost
This is a non-UI component to which you can bind a Dataverse record (`Entity`). Then you can bind column specific UI controls to the RecordHost component. If the column bound controls are not readonly, they will have full two-way-binding to the record, providing both view and editing capabilities.

The `XRMRecordHost` component exposes a Save method to push changes made in the UI controls back to Dataverse.
### Properties
* **Service** This is an [`IOrganizationService`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475) connected to the Dataverse backend.
* **Record** An object of type [`Entity`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) to bind.
* **LogicalName** A string representing the `logicalname` of bound `Record`. If set in combination with `Id` the `Record` will be automatically retrieved by the component.
* **Id** Guid of the `Record` to retrieve, must be used in combination with `LogicalName` when setting.
* **ChangedColumns** A list of column names that have pending changes compared to the bound record.
* **Item[columnname]** Indexer for columns on the bound record. Use this to set values, which will then be added to the `ChangedColumns` and used to create/update the record when calling `SaveChanges`. Use this to read column values programmatically, values returned will be the pending value if available, otherwise the original value from the bound record. All bound control writes to the indexer automatically as the user changes values.
### Methods
* **SuspendLayout** Prevents refresh of data and metadata for bound controls until `ResumeLayout` is called. May be used when several properties shall be changed for the `XRMRecordHost` and its bound controls to prevent flickering and unnecessary calls to the backend.
* **ResumeLayout** Resumes refresh of all bound controls after being suspended using `SuspendLayout`.
* **SaveChanges** Saves the bound record to Dataverse. The change will only include the columns that are updated according to `ChangedColumns`.
* **UndoChanges** Clears the list of pending changes and issues a Refresh to all bound controls to show the original values from the bound record.
### Events
* **ColumnValueChanged** Event invoked when the user updates the value in any bound control, or if a column value is updated through code by setting through the indexer.

---
&nbsp;
## XRMColumnText
Just like any TextBox, but shows values from a record in Dataverse. The control shall be bound to an `XRMRecordHost`, and a `Column` or `DisplayFormat` specified.
### Properties
* **RecordHost** set to a `XRMRecordHost` instance to bind the textbox to.
* **Column** the `logicalname` of the column on the `RecordHost` `Record` to bind the control to. Setting this property is required for edit mode with two way binding. Cannot be used in combination with `DisplayFormat`.
* **DisplayFormat** set to any [**XRM Token**🔗](https://jonasr.app/xrm-tokens/) to define how each record is presented. Cannot be used in combination with `Column`.

---
&nbsp;
## XRMColumnOptionSet
Just like any [`ComboBox`🔗](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox?WT.mc_id=BA-MVP-5002475), but shows values from an OptionSet / Choice column on a record in Dataverse. The control *may* be bound to an `XRMRecordHost`, and a `Column` specified to automatically load available options and select the correct option from the bound record/column.

This control may also be unbound (`RecordHost` is null) and the `DataSource` may then be set to a collection of [`OptionMetadata`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.optionmetadata?WT.mc_id=BA-MVP-5002475) or an [`OptionSetMetadata`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.optionsetmetadata?WT.mc_id=BA-MVP-5002475) instance to populate with any optionset.
### Properties
* **RecordHost** set to a `XRMRecordHost` instance to bind the checkbox to.
* **Column** the `logicalname` of the column on the `RecordHost` `Record` to bind the control to.
* **Sorted** true to sort options alphabetically, false to keep sorting from metadata.
* **ShowValue** true to show the numeric value after the label: "*Customer (42)*"
* **DataSource** set to a collection of [`OptionMetadata`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.optionmetadata?WT.mc_id=BA-MVP-5002475) or an [`OptionSetMetadata`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.optionsetmetadata?WT.mc_id=BA-MVP-5002475) instance to populate with any optionset, overriding `RecordHost` and `Column`.
* **SelectedOption** readonly property containing currently selected [`OptionMetadata`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.optionmetadata?WT.mc_id=BA-MVP-5002475).

---
&nbsp;
## XRMColumnBool
Just like any [`CheckBox`🔗](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.checkbox?WT.mc_id=BA-MVP-5002475), but shows values from a bool / two option / yes/no column on a record in Dataverse. The control shall be bound to an `XRMRecordHost`, and a `Column` specified.
### Properties
* **RecordHost** set to a `XRMRecordHost` instance to bind the checkbox to.
* **Column** the `logicalname` of the column on the `RecordHost` `Record` to bind the control to.
* **ShowMetadataLabels** set this to update the `Text` of the checkbox to the user localized label of the [`TrueOption`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.booleanoptionsetmetadata.trueoption?WT.mc_id=BA-MVP-5002475) or [`FalseOption`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.booleanoptionsetmetadata.falseoption?WT.mc_id=BA-MVP-5002475) as the state changes.

---
&nbsp;
## XRMColumnLookup
Just like any [`ComboBox`🔗](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox?WT.mc_id=BA-MVP-5002475), but shows records from Dataverse. The control *may* be bound to an `XRMRecordHost`, and a `Column` specified to automatically load records based on the target table of the bound record/column.

This control may also be unbound (`RecordHost` is null) and the `DataSource` may then be set to a collection of [`Entity`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) or an [`EntityCollection`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) instance to populate with any records. If a `DataSource` is set, the `Service` property must also be set.

To get the more classic behavior of lookups with a readonly textbox and a button to open a new lookup dialog, a `XRMColumnLookup` bound to a dedicated `XRMRecordHost` can be used in combination with a `Button` and a `XRMLookupDialog` component (see below). This can be useful if the number of lookup records is too large to get a good UX with a combobox.
### Properties
* **RecordHost** set to a `XRMRecordHost` instance to bind the combobox to.
* **Column** the `logicalname` of the column on the `RecordHost` `Record` to bind the control to. When `Record` of the `RecordHost` and the `Column` is set, the control will automatically load records of the target type of the lookup column.
* **OnlyActiveRecords** true to filter loaded records by `statecode eq 0`. Caution when binding to lookups targeting tables with uncommon or not existing statecodes, this will explode 💥 if failing.
* **Filter** additional [`FilterExpression`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.query.filterexpression?WT.mc_id=BA-MVP-5002475) to add to the query when loading records based on bound `RecordHost` and `Column`.
* **Sorted** true to sort lookup records alphabetically according to current `DisplayFormat`.
* **DisplayFormat** set to any [**XRM Token**🔗](https://jonasr.app/xrm-tokens/) to define how each record is presented. Leave blank to display the primary name of the lookup records (just like OOB behavior in the platform).
* **DataSource** set to a collection of [`Entity`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) or an [`EntityCollection`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) instance to populate with any records., overriding `RecordHost` and `Column`.
* **Service** This is an [`IOrganizationService`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475) connected to the Dataverse backend. Must be set when control is unbound and setting `DataSource`.
 
---
&nbsp;
## XRMDataGridView
Just like any DataGridView, but shows data (records) from Dataverse. The control accepts [`EntityCollection`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) or `IEnumerable<Entity>` as `DataSource`.
### Properties
* **Service** This is an [`IOrganizationService`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475) connected to the Dataverse backend.
* **DataSource** set to a collection of [`Entity`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) or an [`EntityCollection`🔗](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entitycollection?WT.mc_id=BA-MVP-5002475) instance to populate the DataGridView with Dataverse data.
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
* **EntityName**
* **SelectedRowRecords**
* **SelectedCellRecords**

### Events
* **RecordClick**
* **RecordDoubleClick**
* **RecordEnter**
* **RecordLeave**
* **RecordMouseEnter**
* **RecordMouseLeave**


---
&nbsp;
## XRMLookupDialog
Just like a `FileOpenDialog` or `ColorDialog`, but lets the user select an entity record.
### Properties
* **Service** set to an active [`IOrganizationService`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice?WT.mc_id=BA-MVP-5002475).
* **Record** The selected entity record
* **Records** The selected entity records, when multiselect is allowed
* **LogicalName** Logical name of the entity to select from
* **LogicalNames** Logical names of the entity to select from, like a polymorphic lookup
* **MultiSelect** Set true to allow user to select multiple records
* **ShowFriendlyNames** Set false to display records with raw values (Guids, OptionSetValues etc)
* **IncludePersonalViews** Set true to make any personal views available to select from
* **Title** of the dialog window
### Methods
* **ShowDialog** Call this method to open the dialog. Returns [`DialogResult`🔗](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.dialogresult?WT.mc_id=BA-MVP-5002475)
  * OK: Record(s) selected
  * Cancel: Nothing selected
  * Abort: Remove value

---
&nbsp;
## XRMEntityComboBox
Just like any ComboBox, but accepts [`EntityMetadataCollection`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadatacollection?WT.mc_id=BA-MVP-5002475) or `IEnumerable<EntityMetadata>` as `DataSource`.
### Properties
* **Show Friendly Names** determines if Display Name or Logical Name of the entities shall be shown.
* `**SelectedEntity**` read this to get the entity currently selected.

---
&nbsp;
## XRMAttributeComboBox
Just like any ComboBox, but accepts [`EntityMetadata`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata?WT.mc_id=BA-MVP-5002475) or `IEnumerable<AttributeMetadata>` as `DataSource`.
### Properties
* **Show Friendly Names** determines if Display Name or Logical Name of the entities shall be shown.
* `**SelectedAttribute**` read this to get the entity currently selected.

&nbsp;

---
&nbsp;
# XrmSubstituter
This is an extension to [`Entity`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.entity?WT.mc_id=BA-MVP-5002475) used to replace "XRM Tokens" in a text with dynamic data from Dataverse.

Read more at https://jonasr.app/xrm-tokens

&nbsp;

---
&nbsp;

# History

This project stems from tools and snippets collected over a number of years.
Some of the controls have been inherited/migrated from the [xrmtb.XrmToolBox.Controls](https://github.com/jamesnovak/xrmtb.XrmToolBox.Controls) project created and maintained by [@jamesnovak](https://github.com/jamesnovak/). 
This was partially in turn inherited/migrated from [CRMWinForm](https://github.com/rappen/CRMWinForm) by [@rappen](https://github.com/rappen/).
