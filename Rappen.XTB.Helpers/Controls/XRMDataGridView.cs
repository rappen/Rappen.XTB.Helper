﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Helpers.Serialization;
using Rappen.XTB.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMDataGridView : DataGridView
    {
        #region Private properties

        private IOrganizationService organizationService;
        private IEnumerable<Entity> entities;
        private EntityMetadata entitymeta;
        private bool autoRefresh = true;
        private bool showFriendlyNames = false;
        private bool showBothNames = false;
        private bool showIdColumn = true;
        private bool showIndexColumn = true;
        private bool showLocalTimes = false;
        private bool? designedColumnsUsed = null;
        private string[] columnOrder = new string[] { };
        private bool showAllColumnsInColumnOrder = false;
        private bool showColumnsNotInColumnOrder = true;
        private DataGridViewColumn[] designedColumns;
        private Dictionary<string, int> columnswidths;
        private string sortColumn;
        private ListSortDirection sortDirection = ListSortDirection.Ascending;
        private Filtering filtering = new Filtering();

        // Obsolete
        private List<string> filterColumns = new List<string>();
        private string filterText = null;

        // Consts
        private const string _extendedMetaAttribute = "MetaAttribute";
        private const string _extendedMetaEntity = "MetaEntity";
        private const string _originalType = "OriginalType";

        #endregion Private properties

        #region Constructor

        public XRMDataGridView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            InitializeComponent();
            base.ReadOnly = true;
            base.AllowUserToAddRows = false;
            base.AllowUserToDeleteRows = false;
            AllowUserToOrderColumns = true;
            AllowUserToResizeRows = false;
            CellClick += HandleClick;
            CellDoubleClick += HandleDoubleClick;
            CellEnter += HandleCellEnter;
            CellLeave += HandleCellLeave;
            CellMouseEnter += HandleCellMouseEnter;
            CellMouseLeave += HandleCellMouseLeave;
            ColumnWidthChanged += HandleColumnWidthChanged;
        }

        #endregion Constructor

        #region Published properties

        [Category("Rappen XRM")]
        [Browsable(false)]
        public new bool ReadOnly { get; set; } = true;

        [Category("Rappen XRM")]
        [Browsable(false)]
        public new bool AllowUserToAddRows { get; set; } = false;

        [Category("Rappen XRM")]
        [Browsable(false)]
        public new bool AllowUserToDeleteRows { get; set; } = false;

        [Category("Rappen XRM")]
        [Description("Indicates the source of data (IEnumerable<Entity> or EntityCollection) for the CRMGridView control.")]
        [Browsable(false)]
        public new object DataSource
        {
            get
            {
                if (entities != null)
                {
                    return entities;
                }
                return base.DataSource;
            }
            set
            {
                designedColumns = new DataGridViewColumn[Columns.Count];
                Columns.CopyTo(designedColumns, 0);

                if (value is EntityCollection entityCollection)
                {
                    entities = entityCollection.Entities;
                    EntityName = entityCollection.EntityName;
                }
                else if (value is IEnumerable<Entity> entitylist)
                {
                    entities = entitylist;
                    EntityName = entities?.FirstOrDefault(e => !string.IsNullOrEmpty(e.LogicalName))?.LogicalName ?? string.Empty;
                }
                else
                {
                    entities = null;
                    EntityName = string.Empty;
                    base.DataSource = value;
                }
                if (entities?.Where(e => !string.IsNullOrEmpty(e.LogicalName)).Select(e => e.LogicalName).Distinct().Count() > 1)
                {
                    throw new ArgumentException("DataSource can only contain entities of the same type.");
                }
                entitymeta = organizationService.GetEntity(entities?.FirstOrDefault(e => !string.IsNullOrEmpty(e.LogicalName))?.LogicalName);
                if (DesignedColumnsUsed() && designedColumns != null)
                {
                    foreach (var col in designedColumns)
                    {
                        if (!Columns.Contains(col.Name))
                        {
                            col.DisplayIndex = Columns.Count;
                            Columns.Add(col);
                        }
                    }
                }
                if (entities != null && autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [Browsable(false)]
        public string LayoutXML
        {
            get => ControlUtils.GetLayoutXMLFromCells(entitymeta, columnswidths);
            set
            {
                columnswidths = ControlUtils.GetCellsFromLayoutXML(value);
                if (entities != null && autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("Specify if content shall be automatically refreshed when datasource, service, flags etc are changed.")]
        public bool AutoRefresh
        {
            get { return autoRefresh; }
            set
            {
                if (value != autoRefresh)
                {
                    autoRefresh = value;
                    if (autoRefresh)
                    {
                        Refresh();
                    }
                    Size = new Size(Width, Height); // Force a refresh of the layout
                }
            }
        }

        [Category("Rappen XRM")]
        [Description("Details for filtering after records are returned from Dataverse.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Browsable(true)]
        public Filtering Filtering
        {
            get => filtering ?? new Filtering();
            set
            {
                filtering = value;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Obsolete("Use Filtering instead.")]
        [Category("Rappen XRM")]
        [DisplayName("Obsolete: Filter Columns")]
        [DefaultValue(null)]
        [Description("Use Filtering instead. Columns to investigate when filtering rows with FilterText. Enter datacolumn name separated by comma.")]
        public string FilterColumns
        {
            get => filterColumns == null ? string.Empty : string.Join(", ", filterColumns);
            set
            {
                var newfilterColumns = string.IsNullOrWhiteSpace(value) ? new List<string>() :
                       value.ToLowerInvariant().Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();
                if (string.Join(",", newfilterColumns) == string.Join(",", filterColumns))
                {
                    return;
                }
                filterColumns = newfilterColumns;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Obsolete("Use Filtering instead.")]
        [Category("Rappen XRM")]
        [DisplayName("Obsolete: Filter Text")]
        [DefaultValue(null)]
        [Description("Use Filtering instead. Text to search for in FilterColumns to filter visible rows in the grid.")]
        public string FilterText
        {
            get => filterText;
            set
            {
                if (filterText == value?.ToLowerInvariant())
                {
                    return;
                }
                filterText = value.ToLowerInvariant();
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(null)]
        [Description("Comma separated list of column names to arrange column order by. Valid only for generic column layout.")]
        public string ColumnOrder
        {
            get => columnOrder == null ? string.Empty : string.Join(", ", columnOrder);
            set
            {
                var lastvalue = columnOrder != null ? string.Join(",", columnOrder) : string.Empty;
                columnOrder = value?.Split(',', '\n').Select(c => c.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToArray() ?? new string[] { };
                showAllColumnsInColumnOrder = showAllColumnsInColumnOrder && columnOrder?.Length > 0;
                showColumnsNotInColumnOrder = showColumnsNotInColumnOrder || columnOrder?.Length == 0;
                if (autoRefresh && !lastvalue.Equals(string.Join(",", columnOrder)))
                {
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("True to show all columns that are listed in ColumnOrder, false to only show populated columns.")]
        public bool ShowAllColumnsInColumnOrder
        {
            get => showAllColumnsInColumnOrder;
            set
            {
                var lastvalue = showAllColumnsInColumnOrder;
                showAllColumnsInColumnOrder = value && columnOrder?.Length > 0;
                if (autoRefresh && !lastvalue.Equals(showAllColumnsInColumnOrder))
                {
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("True to show columns that are in the data but not listed in ColumnOrder, false to ignore them.")]
        public bool ShowColumnsNotInColumnOrder
        {
            get => showColumnsNotInColumnOrder;
            set
            {
                var lastvalue = showColumnsNotInColumnOrder;
                showColumnsNotInColumnOrder = value || columnOrder?.Length == 0;
                if (autoRefresh && !lastvalue.Equals(showColumnsNotInColumnOrder))
                {
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("True to show timestamps in local time, false to show UTC. Only valid when ShowFriendlyNames is true.")]
        public bool ShowLocalTimes
        {
            get { return showLocalTimes; }
            set
            {
                if (value != showLocalTimes)
                {
                    showLocalTimes = value;
                    if (autoRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("True to show friendly names, False to show logical names and guid etc.")]
        public bool ShowFriendlyNames
        {
            get { return showFriendlyNames; }
            set
            {
                if (value != showFriendlyNames)
                {
                    showFriendlyNames = value;
                    if (autoRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("True to show both friendly and technical names (when needed).")]
        public bool ShowBothNames
        {
            get { return showBothNames; }
            set
            {
                if (value != showBothNames)
                {
                    showBothNames = value;
                    if (autoRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("Set this to show the id of each record first in the grid.")]
        public bool ShowIdColumn
        {
            get { return showIdColumn; }
            set
            {
                if (value != ShowIdColumn)
                {
                    showIdColumn = value;
                    if (autoRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(true)]
        [Description("Set this to display a counter column first in the grid.")]
        public bool ShowIndexColumn
        {
            get { return showIndexColumn; }
            set
            {
                if (value != showIndexColumn)
                {
                    showIndexColumn = value;
                    if (autoRefresh)
                    {
                        Refresh();
                    }
                }
            }
        }

        [Category("Rappen XRM")]
        [DefaultValue(false)]
        [Description("Set this to give EntityReference cells a clickable appearance.")]
        public bool EntityReferenceClickable { get; set; } = false;

        public bool SettingsWidths { get; private set; } = false;

        public bool Refreshing { get; private set; } = false;

        #endregion Published properties

        #region Published events

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordClick;

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordDoubleClick;

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordEnter;

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordLeave;

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordMouseEnter;

        [Category("Rappen XRM")]
        public event XRMRecordEventHandler RecordMouseLeave;

        #endregion Published events

        #region Public properties

        [Browsable(false)]
        public IOrganizationService Service
        {
            get { return organizationService; }
            set
            {
                organizationService = value;
                if (autoRefresh)
                {
                    Refresh();
                }
            }
        }

        [Browsable(false)]
        public string EntityName { get; private set; } = string.Empty;

        /// <summary>
        /// IEnumerable<Entity> representing currently selected rows
        /// </summary>
        [Browsable(false)]
        public IEnumerable<Entity> SelectedRowRecords
        {
            get
            {
                if (entities == null)
                {
                    return null;
                }
                var result = new List<Entity>();
                foreach (DataGridViewRow row in SelectedRows.OfType<DataGridViewRow>().OrderBy(r => r.Index))
                {
                    var entity = row.Cells["#entity"].Value as Entity;
                    if (entity != null)
                    {
                        result.Add(entity);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// IEnumerable<Entity> representing all currently selected cells
        /// </summary>
        [Browsable(false)]
        public IEnumerable<Entity> SelectedCellRecords
        {
            get
            {
                if (entities == null)
                {
                    return null;
                }
                var result = new List<Entity>();
                foreach (DataGridViewCell cell in SelectedCells)
                {
                    if (cell.RowIndex >= 0 && cell.RowIndex < Rows.Count)
                    {
                        var row = Rows[cell.RowIndex];
                        var entity = row.Cells["#entity"].Value as Entity;
                        if (entity != null && !result.Contains(entity))
                        {
                            result.Add(entity);
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// IEnumerable<SelectedXRMCell> representing all currently selected cells/entities/rows/columns
        /// </summary>
        [Browsable(false)]
        public IEnumerable<SelectedXRMCell> SelectedXRMCells
        {
            get
            {
                if (entities == null)
                {
                    return null;
                }
                var result = new List<SelectedXRMCell>();
                foreach (DataGridViewCell cell in SelectedCells)
                {
                    if (cell.Visible && cell.RowIndex >= 0 && cell.RowIndex < Rows.Count)
                    {
                        var row = Rows[cell.RowIndex];
                        var entity = row.Cells["#entity"].Value as Entity;
                        if (entity != null &&
                            cell.ColumnIndex >= 0 &&
                            cell.ColumnIndex < Columns.Count &&
                            Columns[cell.ColumnIndex] is DataGridViewColumn col)
                        {
                            var colname = col.Name.Split('|')[0];
                            result.Add(new SelectedXRMCell
                            {
                                RecordRef = entity.ToEntityReference(),
                                Row = row,
                                Column = col,
                                Attribute = colname,
                                Value = entity.Contains(colname) ? entity[colname] : null
                            });
                        }
                    }
                }
                return result;
            }
        }

        #endregion Public properties

        #region Public methods

        /// <summary>
        /// Gets the DataSource object as requested type.
        /// For the CRMGridView the primary expected types T are IEnumerable&lt;Entity&gt; or EntityCollection or DataTable.
        /// </summary>
        /// <typeparam name="T">Type of the DataSource to return.</typeparam>
        /// <returns>DataSource of type T if available, otherwise null.</returns>
        public T GetDataSource<T>()
        {
            if (typeof(T) == typeof(EntityCollection))
            {
                var ec = new EntityCollection { EntityName = EntityName };
                ec.Entities.AddRange(entities);
                return (T)(object)ec;
            }
            else if (typeof(T) == typeof(IEnumerable<Entity>))
            {
                return (T)(object)entities;
            }

            return (T)base.DataSource;
        }

        public Entity GetXRMEntity(int RowIndex)
        {
            if (RowIndex == -1 || !Columns.Contains("#entity"))
            {
                return null;
            }
            var row = Rows[RowIndex];
            var entity = row.Cells["#entity"]?.Value as Entity;
            return entity;
        }

        public object GetXRMValue(int RowIndex, int ColumnIndex)
        {
            var entity = GetXRMEntity(RowIndex);
            var attribute = ColumnIndex >= 0 ? Columns[ColumnIndex].Name : string.Empty;
            return entity != null && entity.Contains(attribute) ? entity[attribute] : null;
        }

        /// <summary>
        /// Refresh the contents of the gridview based on associated Entities and flags
        /// </summary>
        public override void Refresh()
        {
            try
            {
                Refreshing = true;
                if (entities != null)
                {
                    try
                    {
                        var cols = GetTableColumns(entities);
                        var data = GetDataTable(entities, cols);
                        BindData(data);
                        ArrangeColumns();
                        SetIndexAndWidths();
                    }
                    catch (Exception ex)
                    {
                        MessageBoxEx.Show(this, $"Oops.\nUnexpected error during refresh of {this.Name}.\n\nJust try again, please.\nToo many of these messages? Create an issue so Jonas can try again to fix this error.\n\n{ex.Message}");
                    }
                }
                base.Refresh();
            }
            finally
            {
                Refreshing = false;
            }
        }

        /// <summary>
        /// If you have a fetchxml (and maybe layoutxml too) you can set the DataSource by just call this method
        /// and send the fetchxml (and maybe layoutxml) which is automatically retrieved and displaying.
        /// </summary>
        /// <param name="fetchxml">FetchXML must be added, or just null the DataSource.</param>
        /// <param name="layoutxml">If this is added, it will show like layout in the Maker Portal (or in FXB)</param>
        /// <exception cref="Exception"></exception>
        /// <remarks>Remember that the Service must be set before calling this.</remarks>
        public void SetDataSource(string fetchxml, string layoutxml = null)
        {
            if (Service == null)
            {
                throw new Exception("Service is not specified.");
            }
            if (string.IsNullOrEmpty(fetchxml))
            {
                return;
            }
            var result = Service.RetrieveMultiple(new FetchExpression(fetchxml));
            MethodInvoker mi = delegate
            {
                LayoutXML = layoutxml;
                DataSource = result;
            };
            if (InvokeRequired)
            {
                Invoke(mi);
            }
            else
            {
                mi();
            }
        }

        public override void Sort(DataGridViewColumn dataGridViewColumn, ListSortDirection direction)
        {
            if (dataGridViewColumn != null && DataSource is IEnumerable<Entity> data)
            {
                sortDirection = dataGridViewColumn.Name == sortColumn && sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                data = data.OrderBy(d => d.PropertyAsBaseType(dataGridViewColumn.DataPropertyName, null, true));
                DataSource = sortDirection == ListSortDirection.Descending ? data.Reverse() : data;
                if (!autoRefresh)
                {
                    Refresh();
                }
                if (Columns.Contains(dataGridViewColumn.Name))
                {
                    Columns[dataGridViewColumn.Name].HeaderCell.SortGlyphDirection = sortDirection == ListSortDirection.Descending ? SortOrder.Descending : SortOrder.Ascending;
                }
                sortColumn = dataGridViewColumn.Name;
            }
            else
            {
                base.Sort(dataGridViewColumn, direction);
            }
        }

        /// <summary>
        /// Generates some smart info of all selected cells
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetSelectedDetails()
        {
            var details = new Dictionary<string, string>();
            try
            {
                if (SelectedCells.Count > 1)
                {
                    var cellvalues = SelectedXRMCells;
                    var rows = cellvalues.Select(c => c.Row.Index).Distinct().Count();
                    var cols = cellvalues.Select(c => c.Column.Index).Distinct().Count();
                    if (rows > 1 && cols > 1)
                    {
                        details.Add("Selected", $"{rows}R x {cols}C");
                    }
                    else if (rows > 1 && cols == 1)
                    {
                        details.Add("Selected", $"{rows}R{Environment.NewLine}{cellvalues.FirstOrDefault().Column.HeaderText}");
                        if (cellvalues.Count() > 1 && !cellvalues.All(c => c?.Value == null))
                        {
                            if (cellvalues.All(v => v?.Value == null || v.Value is int))
                            {
                                var ints = cellvalues.Where(v => v?.Value != null).Select(v => Convert.ToInt64((int)v.Value));
                                details.Add("Min", ints.Min().ToString());
                                details.Add("Avg", ints.Average().ToString());
                                details.Add("Max", ints.Max().ToString());
                                details.Add("Sum", ((Int64)ints.Sum()).ToString());
                            }
                            else if (cellvalues.All(v => v?.Value == null || v.Value is decimal))
                            {
                                var decs = cellvalues.Where(v => v?.Value != null).Select(v => (decimal)v.Value);
                                details.Add("Min", decs.Min().ToString("F"));
                                details.Add("Avg", decs.Average().ToString("F"));
                                details.Add("Max", decs.Max().ToString("F"));
                                details.Add("Sum", decs.Sum().ToString("F"));
                            }
                            else if (cellvalues.All(v => v?.Value == null || v.Value is Money))
                            {
                                var mons = cellvalues.Where(v => v?.Value != null).Select(v => (Money)v.Value);
                                details.Add("Min", mons.Min(m => m.Value).ToString("C"));
                                details.Add("Avg", mons.Average(m => m.Value).ToString("C"));
                                details.Add("Max", mons.Max(m => m.Value).ToString("C"));
                                details.Add("Sum", mons.Sum(m => m.Value).ToString("C"));
                            }
                            else if (cellvalues.All(v => v?.Value == null || v.Value is DateTime))
                            {
                                var dates = cellvalues.Where(v => v?.Value != null).Select(v => (DateTime)v.Value);
                                details.Add("First", dates.Min().ToShortDateString());
                                details.Add("Last", dates.Max().ToShortDateString());
                            }
                            details.Add("Unique", cellvalues.Select(v => EntitySerializer.AttributeToBaseType(v?.Value)).Distinct().Count().ToString());
                        }
                    }
                    else if (rows == 1 && cols > 1)
                    {
                        details.Add("Selected", $"{cols}C");
                    }
                    var nulls = cellvalues.Count(v => v?.Value == null);
                    if (nulls > 0)
                    {
                        details.Add("Empty", nulls.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<string, string> { { "Error", ex.Message } };
            }
            return details;
        }

        public void Filter() => Refresh();

        #endregion Public methods

        #region Private event handler methods

        private void HandleClick(object sender, DataGridViewCellEventArgs e)
        {
            GetXRMRecordEventArgs(e).OnRecordEvent(this, RecordClick);
        }

        private void HandleDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                GetXRMRecordEventArgs(e).OnRecordEvent(this, RecordDoubleClick);
            }
        }

        private void HandleCellEnter(object sender, DataGridViewCellEventArgs e)
        {
            GetXRMRecordEventArgs(e).OnRecordEvent(this, RecordEnter);
        }

        private void HandleCellLeave(object sender, DataGridViewCellEventArgs e)
        {
            GetXRMRecordEventArgs(e).OnRecordEvent(this, RecordLeave);
        }

        private void HandleCellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (EntityReferenceClickable)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                {
                    return;
                }
                var entity = GetXRMEntity(e.RowIndex);
                var row = Rows[e.RowIndex];
                var col = Columns[e.ColumnIndex];
                if (!entity.Contains(col.Name))
                {
                    return;
                }
                var value = entity[col.Name];
                if (value is EntityReference)
                {
                    var font = new Font(Font, FontStyle.Underline);
                    var cell = row.Cells[e.ColumnIndex];
                    cell.Style.Font = font;
                    Cursor = Cursors.Hand;
                }
            }
            GetXRMRecordEventArgs(e).OnRecordEvent(this, RecordMouseEnter);
        }

        private void HandleCellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (EntityReferenceClickable)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                {
                    return;
                }
                var entity = GetXRMEntity(e.RowIndex);
                var row = Rows[e.RowIndex];
                var col = Columns[e.ColumnIndex];
                if (!entity.Contains(col.Name))
                {
                    return;
                }
                var value = entity[col.Name];
                if (value is EntityReference)
                {
                    var font = new Font(Font, FontStyle.Regular);
                    var cell = row.Cells[e.ColumnIndex];
                    cell.Style.Font = font;
                    Cursor = Cursors.Default;
                }
            }
            GetXRMRecordEventArgs(e).OnRecordEvent(this, RecordMouseLeave);
        }

        private void HandleColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (SettingsWidths)
            {
                return;
            }
            if (DesignedColumnsUsed())
            {
                return;
            }
            columnswidths = Columns.Cast<DataGridViewColumn>()
                .Where(c => !string.IsNullOrEmpty(c.Name) && !c.Name.StartsWith("#") && !c.Name.EndsWith("|both") && c.Visible && c.Width > 5)
                .OrderBy(c => c.DisplayIndex)
                .ToDictionary(c => c.Name, c => c.Width);
        }

        #endregion Private event handler methods

        #region Private methods

        private bool DesignedColumnsUsed()
        {
            if (designedColumnsUsed == null)
            {
                designedColumnsUsed = Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && c.Width > 5).Any();
                if (designedColumnsUsed == true)
                {
                    designedColumns = new DataGridViewColumn[Columns.Count];
                    Columns.CopyTo(designedColumns, 0);
                }
                else
                {
                    designedColumns = null;
                }
            }
            return designedColumnsUsed == true;
        }

        private XRMRecordEventArgs GetXRMRecordEventArgs(DataGridViewCellEventArgs e)
        {
            var entity = GetXRMEntity(e.RowIndex);
            var attribute = e.ColumnIndex >= 0 ? Columns[e.ColumnIndex].Name : string.Empty;
            var args = new XRMRecordEventArgs(entity, attribute);
            return args;
        }

        private List<DataColumn> GetTableColumns(IEnumerable<Entity> entities)
        {
            var columns = new List<DataColumn>();
            if (DesignedColumnsUsed() == true)
            {
                PopulateColumnsFromDesign(entities, columns);
            }
            else
            {
                Columns.Clear();
                columns.Add(new DataColumn("#no", typeof(int)) { Caption = "#", AutoIncrement = true, AutoIncrementSeed = 1 });
                columns.Add(new DataColumn("#id", typeof(Guid)) { Caption = "Id" });

                var attributes = new List<string>(columnOrder);
                if (showColumnsNotInColumnOrder || attributes.Count == 0)
                {
                    attributes.AddRange(entities
                        .SelectMany(e => e.Attributes)
                        .Select(a => a.Key)
                        .Distinct());
                }
                attributes.Distinct().ToList().ForEach(a =>
                {
                    // Force the column to be displayed if it's in the ColumnOrder list and we either want to display
                    // all the listed columns or it contains data
                    var force = columnOrder.Contains(a) && (showAllColumnsInColumnOrder || entities.Any(e => e.Contains(a)));

                    AddColumnForAttribute(entities, columns, a, force, showFriendlyNames);
                    if (showBothNames)
                    {
                        var rawentity = EntityName;
                        var rawattribute = a;
                        if (a.Contains("."))
                        {
                            var value = GetFirstValueForAttribute(entities, a);
                            if (value != null)
                            {
                                var meta = organizationService.GetAttribute(EntityName, a, value);
                                if (meta?.EntityLogicalName != null)
                                {
                                    rawentity = meta.EntityLogicalName;
                                    rawattribute = meta.LogicalName;
                                }
                            }
                        }
                        if (organizationService.GetFriendlyAttributeIsNotAsRawValue(rawentity, rawattribute))
                        {
                            AddColumnForAttribute(entities, columns, a, force, !showFriendlyNames);
                        }
                    }
                });
            }
            if (!columns.Any(c => c.ColumnName.Equals("#entity")))
            {
                columns.Add(new DataColumn("#entity", typeof(Entity)));
            }
            return columns;
        }

        private void PopulateColumnsFromDesign(IEnumerable<Entity> entities, List<DataColumn> columns)
        {
            foreach (DataGridViewColumn viewcol in Columns)
            {
                if (viewcol.Name == "#entity")
                {
                    continue;
                }
                if (string.IsNullOrEmpty(viewcol.DataPropertyName))
                {
                    viewcol.DataPropertyName = viewcol.Name;
                }
                var attribute = viewcol.DataPropertyName;
                var dataColumn = CreateColumnForAttribute(entities, attribute, true, showFriendlyNames);
                if (!string.IsNullOrEmpty(viewcol.DefaultCellStyle.Format))
                {
                    if (dataColumn.ExtendedProperties.Contains("Format"))
                    {
                        dataColumn.ExtendedProperties["Format"] = viewcol.DefaultCellStyle.Format;
                    }
                    else
                    {
                        dataColumn.ExtendedProperties.Add("Format", viewcol.DefaultCellStyle.Format);
                    }
                }
                dataColumn.Caption = viewcol.HeaderText;
                columns.Add(dataColumn);
            }
        }

        private DataColumn CreateColumnForAttribute(IEnumerable<Entity> entities, string attribute, bool force, bool friendlyname)
        {
            var value = GetFirstValueForAttribute(entities, attribute);
            if (value == null && !force)
            {
                return null;
            }
            var type = GetValueType(value, friendlyname);
            var dataColumn = new DataColumn(attribute + (friendlyname != showFriendlyNames ? "|both" : ""), type);
            dataColumn.SetFriendly(friendlyname);
            var meta = organizationService.GetAttribute(EntityName, attribute, value);
            dataColumn.ExtendedProperties.Add(_extendedMetaAttribute, meta);
            if (value is AliasedValue aliasvalue && aliasvalue.EntityLogicalName is string aliasentityname && !string.IsNullOrEmpty(aliasentityname))
            {
                var aliasentitymeta = organizationService.GetEntity(aliasentityname);
                dataColumn.ExtendedProperties.Add(_extendedMetaEntity, aliasentitymeta);
            }
            dataColumn.ExtendedProperties.Add(_originalType, GetInnerValueType(value));
            if (meta is DateTimeAttributeMetadata && entities.Any(e => e.Contains(attribute) && e[attribute] is DateTime dtvalue && dtvalue.Millisecond > 0))
            {
                dataColumn.ExtendedProperties.Add("Format", "yyyy-MM-dd HH:mm:ss.fff");
            }
            return dataColumn;
        }

        private static Type GetInnerValueType(object value)
        {
            if (value is AliasedValue aliasedValue)
            {
                return GetInnerValueType(aliasedValue.Value);
            }
            return value != null ? value.GetType() : null;
        }

        private Type GetValueType(object value, bool friendly)
        {
            if (value == null)
            {
                return typeof(string);
            }
            if (friendly || !ValueTypeIsFriendly(value))
            {
                return typeof(string);
            }
            var basevalue = EntitySerializer.AttributeToBaseType(value);
            if (basevalue == null)
            {
                return typeof(string);
            }
            return basevalue.GetType();
        }

        private Type GetOriginalType(DataGridViewColumn dataGridViewColumn)
        {
            if (base.DataSource is DataTable table &&
                table.Columns.Contains(dataGridViewColumn.DataPropertyName) &&
                table.Columns[dataGridViewColumn.DataPropertyName] is DataColumn dataColumn)
            {
                return dataColumn.ExtendedProperties.ContainsValue(_originalType) ? dataColumn.ExtendedProperties[_originalType] as Type : null;
            }
            return null;
        }

        private bool ValueTypeIsFriendly(object value)
        {
            return value is Int32 || value is decimal || value is double || value is string || value is Money || value is DateTime;
        }

        private void AddColumnForAttribute(IEnumerable<Entity> entities, List<DataColumn> columns, string attribute, bool force, bool friendlyname)
        {
            if (columns.Any(c => c.ColumnName.Equals(attribute) && c.GetFriendly() == friendlyname))
            {   // Column already added for some reason
                return;
            }
            if (CreateColumnForAttribute(entities, attribute, force, friendlyname) is DataColumn dataColumn && dataColumn != null)
            {
                var meta = GetAttributeMetadata(dataColumn);
                if (meta?.IsPrimaryId == true && (!force || ShowIdColumn && meta.LogicalName == attribute))
                {
                    // Don't show the primary key column twice. Ignore the primary key column if:
                    // * `force` is false, i.e. the user isn't asking to see this column via the ColumnOrder property, OR
                    // * The standard ID column is being already shown (via ShowIdColumn) and this isn't an aliased version
                    //   An aliased column probably indicates an aggregate query and we want to show the aggregate (e.g. count)
                    //   as a different column to the ID of the record.
                    return;
                }
                var columnName = string.Empty;
                if (friendlyname)
                {
                    if (!attribute.Contains(".") && !attribute.Equals(meta?.LogicalName))
                    {   // Should be an aliased attribute
                        columnName = attribute;
                    }
                    else if (meta?.DisplayName?.UserLocalizedLabel?.Label is string label)
                    {
                        columnName = label;
                        if (attribute.Contains("."))
                        {
                            if (dataColumn.ExtendedProperties.ContainsKey(_extendedMetaEntity) && dataColumn.ExtendedProperties[_extendedMetaEntity] is EntityMetadata aliasmeta)
                            {
                                columnName += $" ({aliasmeta.DisplayName.UserLocalizedLabel.Label})";
                            }
                            else
                            {
                                columnName = attribute.Split('.')[0] + " " + columnName;
                            }
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(columnName) && meta?.LogicalName is string logicalname && !attribute.Contains("."))
                {
                    if (meta.EntityLogicalName is string entitylogicalname && entitylogicalname != entitymeta?.LogicalName)
                    {
                        columnName = entitylogicalname + "." + logicalname;
                    }
                    else
                    {
                        columnName = logicalname;
                    }
                }
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    columnName = attribute;
                }
                dataColumn.Caption = columnName;
                columns.Add(dataColumn);
            }
        }

        private AttributeMetadata GetAttributeMetadata(DataGridViewColumn dataGridViewColumn)
        {
            if (base.DataSource is DataTable table &&
                table.Columns.Contains(dataGridViewColumn.DataPropertyName) &&
                table.Columns[dataGridViewColumn.DataPropertyName] is DataColumn dataColumn)
            {
                return GetAttributeMetadata(dataColumn);
            }
            return null;
        }

        private static AttributeMetadata GetAttributeMetadata(DataColumn dataColumn)
        {
            return dataColumn.ExtendedProperties.ContainsKey(_extendedMetaAttribute) ? dataColumn.ExtendedProperties[_extendedMetaAttribute] as AttributeMetadata : null;
        }

        private object GetFirstValueForAttribute(IEnumerable<Entity> entities, string attribute)
        {
            foreach (var entity in entities)
            {
                if (entity.Contains(attribute) && entity[attribute] != null)
                {
                    return entity[attribute];
                }
            }
            return null;
        }

        private DataTable GetDataTable(IEnumerable<Entity> entities, List<DataColumn> columns)
        {
            var dTable = new DataTable();
            dTable.Columns.AddRange(columns.ToArray());
            var filtcols = Filtering?.ColumnList?.Count > 0 ? Filtering.ColumnList : filterColumns;
            var filteredcols = columns.Cast<DataColumn>().Where(c => filtcols == null || filtcols.Contains(c.ColumnName.ToLowerInvariant())).ToList();

            foreach (var entity in entities)
            {
                var dRow = dTable.NewRow();
                foreach (DataColumn column in dTable.Columns)
                {
                    var col = column.ColumnName.Split('|')[0];
                    try
                    {
                        object value = null;
                        if (col == "#no")
                        {   // Sequence column
                            continue;
                        }
                        else if (col == "#id")
                        {
                            value = entity.Id;
                        }
                        else if (col == "#entity")
                        {
                            value = entity;
                        }
                        else if (entity.Contains(col) && entity[col] != null)
                        {
                            value = entity[col];
                            if (EntitySerializer.AttributeToBaseType(value) is DateTime dtvalue && showLocalTimes && (dtvalue).Kind == DateTimeKind.Utc)
                            {
                                value = dtvalue.ToLocalTime();
                            }
                            if (column.GetFriendly())
                            {
                                var format = column.ExtendedProperties["Format"] as string;
                                if ((!ValueTypeIsFriendly(value) || col.IsPOA()) && GetAttributeMetadata(column) is AttributeMetadata meta)
                                {
                                    value = EntitySerializer.AttributeToString(value, meta, format);
                                }
                                else
                                {
                                    value = string.Format("{0:" + format + "}", EntitySerializer.AttributeToBaseType(value));
                                }
                            }
                            else
                            {
                                value = EntitySerializer.AttributeToBaseType(value);
                            }
                        }
                        if (value == null)
                        {
                            value = DBNull.Value;
                        }
                        dRow[column] = value;
                    }
                    catch
                    {
                        MessageBoxEx.Show(this, "Attribute " + col + " failed, value: " + entity[col].ToString());
                    }
                }
                if (FilterIncludeRow(dRow, filteredcols))
                {
                    dTable.Rows.Add(dRow);
                }
            }
            return dTable;
        }

        private bool FilterIncludeRow(DataRow dRow, List<DataColumn> filtercolumns)
        {
            var text = !string.IsNullOrEmpty(Filtering?.Text) ? Filtering.Text : filterText;
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }
            var result = Filtering?.And == true;
            foreach (var column in filtercolumns)
            {
                var cellvalue = dRow[column]?.ToString();
                var colresult = false;
                if (Filtering?.RegEx == true)
                {
                    colresult = Regex.IsMatch(cellvalue, text, RegexOptions.IgnoreCase);
                }
                else
                {
                    colresult = cellvalue.ToLowerInvariant().Contains(text);
                }
                if (Filtering?.And == true)
                {
                    result = result && colresult;
                }
                else
                {
                    result = result || colresult;
                }
            }
            if (Filtering?.Not == true)
            {
                result = !result;
            }
            return result;
        }

        private void BindData(DataTable dTable)
        {
            SuspendLayout();
            base.DataSource = dTable;
            VerifySupportingColumn("#entity");
            foreach (DataGridViewColumn col in Columns)
            {
                var datacolumn = dTable.Columns[col.DataPropertyName];
                col.HeaderText = datacolumn.Caption;
                var type = datacolumn.DataType;
                if (datacolumn.ExtendedProperties.ContainsKey(_originalType))
                {
                    type = datacolumn.ExtendedProperties[_originalType] as Type;
                }
                if (datacolumn.ColumnName.IsPOA() != true || !datacolumn.GetFriendly())
                {
                    if (type == typeof(int) ||
                        type == typeof(long) ||
                        type == typeof(decimal) ||
                        type == typeof(double) ||
                        type == typeof(Money) ||
                        (type == typeof(OptionSetValue) && !datacolumn.GetFriendly()))
                    {
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                }
                if (datacolumn.ExtendedProperties.ContainsKey("Format"))
                {
                    col.DefaultCellStyle.Format = datacolumn.ExtendedProperties["Format"] as string;
                }
                if (datacolumn.ColumnName == "#no")
                {
                    col.Visible = showIndexColumn;
                }
                if (datacolumn.ColumnName == "#id")
                {
                    col.Visible = showIdColumn;
                }
                if (datacolumn.ColumnName == "#entity")
                {
                    col.Visible = false;
                }
            }
            if (AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.None && AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.Fill)
            {
                AutoResizeColumns(AutoSizeColumnsMode);
            }
            ResumeLayout();
        }

        private void VerifySupportingColumn(string col)
        {
            if (Columns.Contains(col))
            {
                return;
            }
            var newcol = new DataGridViewTextBoxColumn
            {
                Name = col,
                DataPropertyName = col,
                HeaderText = col,
                ReadOnly = true
            };
            Columns.Add(newcol);
        }

        private void ArrangeColumns()
        {
            if (columnOrder?.Length == 0 || DesignedColumnsUsed())
            {
                return;
            }
            var maxpos = Columns.Cast<DataGridViewColumn>().Count(c => c.Visible) + 1;
            var pos = 2;
            foreach (var attribute in columnOrder)
            {
                pos = Math.Min(pos, maxpos);
                if (Columns.Contains(attribute))
                {
                    Columns[attribute].DisplayIndex = pos++;
                }
                if (Columns.Contains(attribute + "|both"))
                {
                    Columns[attribute + "|both"].DisplayIndex = pos++;
                }
            }
        }

        private void SetIndexAndWidths()
        {
            if (columnswidths == null || DesignedColumnsUsed())
            {
                return;
            }
            SettingsWidths = true;
            var cellnames = columnswidths.Where(c => c.Value > 0).Select(c => c.Key);
            cellnames.Where(c => !Columns.Contains(c)).ToList().ForEach(c => Columns.Add(c, c));
            // First hidden all columns
            Columns.Cast<DataGridViewColumn>().ToList().ForEach(c => c.Visible = false);
            var display = 0;
            if (!columnswidths.Any(cw => cw.Key == "#no") && Columns["#no"] is DataGridViewColumn colno)
            {
                if (showIndexColumn)
                {
                    colno.DisplayIndex = display++;
                }
                colno.Width = 40;
                colno.Visible = showIndexColumn;
            }
            if (!columnswidths.Any(cw => cw.Key == "#id") && Columns["#id"] is DataGridViewColumn colid)
            {
                if (showIdColumn)
                {
                    colid.DisplayIndex = display++;
                }
                colid.Width = 100;
                colid.Visible = showIdColumn;
            }
            foreach (var cell in columnswidths.Where(c => c.Value > 0))
            {
                if (Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Name == cell.Key) is DataGridViewColumn column)
                {
                    column.DisplayIndex = display++;
                    column.Width = cell.Value;
                    column.Visible = cell.Value > 0;
                }
                if (showBothNames && Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Name == cell.Key + "|both") is DataGridViewColumn columnboth)
                {
                    columnboth.DisplayIndex = display++;
                    columnboth.Width = cell.Value;
                    columnboth.Visible = cell.Value > 0;
                }
            }
            SettingsWidths = false;
        }

        #endregion Private methods
    }

    public class Filtering
    {
        private List<string> columns { get; set; } = new List<string>();
        [Browsable(false)]
        internal List<string> ColumnList => columns;
        public string Columns
        {
            get => string.Join(", ", columns);
            set
            {
                columns = string.IsNullOrWhiteSpace(value) ? new List<string>() :
                    value.ToLowerInvariant().Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();
            }
        }
        public string Text { get; set; } = string.Empty;
        public bool And { get; set; } = false;
        public bool Not { get; set; } = false;
        public bool RegEx { get; set; } = false;

        public Filtering()
        {
            Columns = string.Empty;
            Text = string.Empty;
            And = false;
            Not = false;
            RegEx = false;
        }

        public override string ToString()
        {
            return $"Columns: {columns?.Count()}, Text: {Text}, And: {And}, Not: {Not}, RegEx: {RegEx}";
        }
    }

    /// <summary>
    /// Class containing the selected cell with informations as:
    ///   EntityReference to the record
    ///   Row in the grid
    ///   Column in the grid
    ///   Attribute name in the record
    ///   Value of the cell
    /// </summary>
    public class SelectedXRMCell
    {
        public EntityReference RecordRef;
        public DataGridViewRow Row;
        public DataGridViewColumn Column;
        public string Attribute;
        public object Value;
    }
}