using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XTB.Helpers.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using XrmToolBox.Extensibility;

namespace Rappen.XTB.Helpers
{
    public static class ExcelHelper
    {
        private const string LinkIcon = "🔗";
        private const int XlHAlignCenter = -4108;
        private const int XlVAlignTop = -4160;
        private const int XlCalculationManual = -4135;
        private const int XlCalculationAutomatic = -4105;
        private const int XlEdgeBottom = 9;
        private const int XlContinuous = 1;
        private const int XlThick = 4;
        private const int XlAnd = 1;

        private static readonly Dictionary<string, string> PrimaryNameCache = new(StringComparer.OrdinalIgnoreCase);

        private static void Try(Action action)
        { try { action(); } catch { /* ignored: optional non-critical */ } }

        public static void ExportToExcel(PluginControlBase tool, XRMDataGridView xrmgrid, bool addLinks, string fetch, string layout, Action afterExport, int? columnMaxWidth = null, bool autoFitRows = false)
        {
            var gridData = CollectGridDataOnUIThread(xrmgrid, addLinks, tool.ConnectionDetail);
            if (gridData == null)
            {
                return;
            }

            tool.WorkAsync(new WorkAsyncInfo
            {
                Message = "Opening in Excel...",
                Work = (w, a) => ExportToExcelDirect(w, tool.ToolName, fetch, layout, tool.ConnectionDetail, gridData, columnMaxWidth, autoFitRows),
                ProgressChanged = p => tool.SetWorkingMessage(p.UserState.ToString()),
                PostWorkCallBack = a =>
                {
                    if (a.Error != null)
                    {
                        tool.ShowErrorDialog(a.Error, "Open Excel");
                    }
                    afterExport?.Invoke();
                }
            });
        }

        private static GridData CollectGridDataOnUIThread(XRMDataGridView xrmgrid, bool addLinks, ConnectionDetail conndet)
        {
            if (xrmgrid == null || xrmgrid.IsDisposed || !xrmgrid.IsHandleCreated)
            {
                return null;
            }

            GridData result = null;
            Exception invokeEx = null;

            void Collect()
            {
                try
                {
                    var visibleCols = xrmgrid.Columns.Cast<DataGridViewColumn>()
                        .Where(c => c.Visible)
                        .OrderBy(c => c.DisplayIndex)
                        .ToList();

                    var primaryCol = addLinks ? GetPrimaryNameColumn(xrmgrid, visibleCols) : null;
                    var primaryAttr = primaryCol?.Name.Split('|')[0];

                    result = new GridData
                    {
                        Headers = visibleCols.Select(c => c.HeaderText).ToList(),
                        Rows = new List<List<CellData>>(),
                        HasLinkColumn = addLinks && primaryCol == null
                    };

                    if (result.HasLinkColumn)
                    {
                        result.Headers.Insert(0, LinkIcon);
                    }

                    for (var r = 0; r < xrmgrid.Rows.Count; r++)
                    {
                        if (!xrmgrid.Rows[r].Visible)
                        {
                            continue;
                        }

                        var entity = addLinks ? xrmgrid.GetXRMEntity(r) : null;
                        var urlPrimary = entity?.GetEntityUrl(conndet);
                        var row = new List<CellData>();

                        if (result.HasLinkColumn)
                        {
                            row.Add(new CellData { Value = LinkIcon, Url = urlPrimary });
                        }

                        foreach (var col in visibleCols)
                        {
                            var value = xrmgrid[col.Index, r]?.Value?.ToString() ?? string.Empty;
                            string url = null;

                            if (addLinks)
                            {
                                var baseName = col.Name.Split('|')[0];
                                var isRootAttr = !baseName.StartsWith("#") && !baseName.Contains(".");

                                if (primaryCol != null && isRootAttr && string.Equals(baseName, primaryAttr, StringComparison.OrdinalIgnoreCase))
                                {
                                    url = urlPrimary;
                                }
                                else if (isRootAttr && entity?.Contains(baseName) == true)
                                {
                                    var val = entity[baseName];
                                    if (val is AliasedValue av)
                                    {
                                        val = av.Value;
                                    }

                                    if (val is EntityReference er)
                                    {
                                        url = er.GetEntityUrl(conndet);
                                    }
                                }
                            }

                            row.Add(new CellData { Value = value, Url = url });
                        }

                        result.Rows.Add(row);
                    }
                }
                catch (Exception ex)
                {
                    invokeEx = ex;
                }
            }

            if (xrmgrid.InvokeRequired)
            {
                xrmgrid.Invoke((MethodInvoker)Collect);
            }
            else
            {
                Collect();
            }

            if (invokeEx != null)
            {
                throw invokeEx;
            }

            return result;
        }

        private static DataGridViewColumn GetPrimaryNameColumn(XRMDataGridView grid, List<DataGridViewColumn> visibleCols)
        {
            if (!TryGetPrimaryNameAttribute(grid, out var primaryAttr))
            {
                return null;
            }

            return visibleCols.FirstOrDefault(c =>
            {
                var baseName = c.Name.Split('|')[0];
                return !baseName.StartsWith("#") && !baseName.Contains(".") &&
                       string.Equals(baseName, primaryAttr, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static void ExportToExcelDirect(System.ComponentModel.BackgroundWorker bw, string toolname, string fetch, string layout, ConnectionDetail conndet, GridData gridData, int? columnMaxWidth, bool autoFitRows)
        {
            bw?.ReportProgress(10, "Starting Excel...");

            Exception excelEx = null;
            var sta = new Thread(() =>
            {
                try
                {
                    dynamic app = CreateExcelApplication();
                    Try(() => app.Visible = false);

                    using (new ExcelOptimization(app))
                    {
                        bw?.ReportProgress(15, "Creating Result sheet...");
                        dynamic wb = app.Workbooks.Add();
                        dynamic resultSheet = wb.Worksheets[1];
                        resultSheet.Name = $"{toolname} - Result";

                        bw?.ReportProgress(20, "Writing data...");
                        WriteGridToSheet(resultSheet, gridData, bw);

                        bw?.ReportProgress(50, "Formatting...");
                        FormatResultSheet(resultSheet, gridData.HasLinkColumn, columnMaxWidth, autoFitRows);

                        bw?.ReportProgress(70, "Adding source info...");
                        PopulateSourceSheet(wb, toolname, fetch, layout, conndet);

                        Try(() => resultSheet.Activate());
                        Try(() => resultSheet.Range["A1"].Select());
                    }

                    bw?.ReportProgress(95, "Opening Excel...");
                    Try(() => app.Visible = true);
                }
                catch (Exception ex)
                {
                    excelEx = ex;
                }
            });

            sta.SetApartmentState(ApartmentState.STA);
            sta.Start();
            sta.Join();

            if (excelEx != null)
            {
                throw excelEx;
            }
        }

        private static void WriteGridToSheet(dynamic sheet, GridData gridData, System.ComponentModel.BackgroundWorker bw)
        {
            var rowCount = gridData.Rows.Count + 1; // +1 for header
            var colCount = gridData.Headers.Count;

            // Build 2D array for bulk write
            var data = new object[rowCount, colCount];

            // Headers
            for (var c = 0; c < colCount; c++)
            {
                data[0, c] = gridData.Headers[c];
            }

            // Data rows
            for (var r = 0; r < gridData.Rows.Count; r++)
            {
                var row = gridData.Rows[r];
                for (var c = 0; c < row.Count; c++)
                {
                    data[r + 1, c] = row[c].Value;
                }
            }

            bw?.ReportProgress(25, "Writing to Excel...");

            // Single bulk write - MUCH faster than cell-by-cell
            var startCell = sheet.Cells[1, 1];
            var endCell = sheet.Cells[rowCount, colCount];
            var range = sheet.Range[startCell, endCell];
            range.Value = data;

            bw?.ReportProgress(35, "Adding hyperlinks...");

            // Add hyperlinks (only for cells that have URLs)
            var linkCount = 0;
            for (var r = 0; r < gridData.Rows.Count; r++)
            {
                var row = gridData.Rows[r];
                for (var c = 0; c < row.Count; c++)
                {
                    if (!string.IsNullOrWhiteSpace(row[c].Url))
                    {
                        Try(() => sheet.Hyperlinks.Add(sheet.Cells[r + 2, c + 1], row[c].Url));
                        linkCount++;
                    }
                }

                if (linkCount > 0 && r % 500 == 0)
                {
                    bw?.ReportProgress(35 + (r * 15 / Math.Max(gridData.Rows.Count, 1)), $"Adding links ({linkCount})...");
                }
            }
        }

        private static void FormatResultSheet(dynamic sheet, bool hasLinkColumn, int? columnMaxWidth, bool autoFitRows)
        {
            if (hasLinkColumn)
            {
                Try(() => sheet.Columns[1].HorizontalAlignment = XlHAlignCenter);
            }

            dynamic header = sheet.Rows[1];
            header.Font.Bold = true;
            header.Borders[XlEdgeBottom].LineStyle = XlContinuous;
            header.Borders[XlEdgeBottom].Weight = XlThick;
            header.AutoFilter(1, Type.Missing, XlAnd, Type.Missing, true);

            Try(() => sheet.Application.ActiveWindow.SplitRow = 1);
            Try(() => sheet.Application.ActiveWindow.FreezePanes = true);
            Try(() => sheet.UsedRange.VerticalAlignment = XlVAlignTop);

            Try(() =>
            {
                dynamic used = sheet.UsedRange;
                used.WrapText = false;
                used.Columns.AutoFit();

                if (columnMaxWidth.HasValue)
                {
                    var maxWidth = (columnMaxWidth.Value - 5) / 7.0;
                    for (int c = 1; c <= used.Columns.Count; c++)
                    {
                        Try(() =>
                        {
                            if (sheet.Columns[c].ColumnWidth > maxWidth)
                            {
                                sheet.Columns[c].ColumnWidth = maxWidth;
                            }
                        });
                    }
                }

                // Store default row height before enabling WrapText
                double defaultRowHeight = sheet.StandardHeight;

                used.WrapText = true;

                if (autoFitRows)
                {
                    used.Rows.AutoFit();
                }
                else
                {
                    // Reset all rows to default height after WrapText expanded them
                    used.Rows.RowHeight = defaultRowHeight;
                }
            });
        }

        private static dynamic CreateExcelApplication()
        {
            var excelType = Type.GetTypeFromProgID("Excel.Application")
                ?? throw new Exception("Microsoft Excel is not installed.");
            return Activator.CreateInstance(excelType);
        }

        private static void PopulateSourceSheet(dynamic wb, string toolname, string fetch, string layout, ConnectionDetail conndet)
        {
            dynamic sheet = wb.Sheets.Add();
            Try(() => sheet.Move(After: wb.Sheets[wb.Sheets.Count]));
            sheet.Name = $"{toolname} - Source";

            var fetchFormatted = string.IsNullOrEmpty(fetch) ? fetch : XDocument.Parse(RemovePagingFromFetch(fetch)).ToString();

            sheet.Cells[1, 1].Value = "Connection";
            sheet.Cells[1, 2].Value = conndet?.ConnectionName;
            sheet.Cells[2, 1].Value = "URL";
            sheet.Cells[2, 2].Value = conndet?.WebApplicationUrl;
            sheet.Cells[3, 1].Value = "Query";
            sheet.Cells[3, 2].Value = fetchFormatted;

            if (!string.IsNullOrEmpty(layout))
            {
                sheet.Cells[4, 1].Value = "Layout";
                sheet.Cells[4, 2].Value = layout;
            }

            sheet.Columns[1].Font.Bold = true;
            Try(() => sheet.Columns[1].AutoFit());
            Try(() => sheet.Columns[2].WrapText = true);
            Try(() => sheet.Columns[2].ColumnWidth = (800 - 5) / 7.0);
            Try(() => sheet.UsedRange.VerticalAlignment = XlVAlignTop);
            Try(() => sheet.UsedRange.Rows.AutoFit());
        }

        private static string RemovePagingFromFetch(string fetch)
        {
            if (string.IsNullOrEmpty(fetch))
            {
                return fetch;
            }

            var fetchType = XRM.Helpers.FetchXML.Fetch.FromString(fetch);
            if (fetchType.PagingCookie != null || fetchType.PageNumber != null)
            {
                fetchType.PagingCookie = null;
                fetchType.PageNumber = null;
                return fetchType.ToString();
            }
            return fetch;
        }

        private static bool TryGetPrimaryNameAttribute(XRMDataGridView grid, out string primaryName)
        {
            primaryName = null;
            if (grid?.Service == null || string.IsNullOrWhiteSpace(grid.EntityName))
            {
                return false;
            }

            if (!PrimaryNameCache.TryGetValue(grid.EntityName, out primaryName))
            {
                primaryName = grid.Service.GetEntity(grid.EntityName)?.PrimaryNameAttribute;
                if (!string.IsNullOrWhiteSpace(primaryName))
                {
                    PrimaryNameCache[grid.EntityName] = primaryName;
                }
            }
            return !string.IsNullOrWhiteSpace(primaryName);
        }

        private class GridData
        {
            public List<string> Headers { get; set; }
            public List<List<CellData>> Rows { get; set; }
            public bool HasLinkColumn { get; set; }
        }

        private class CellData
        {
            public string Value { get; set; }
            public string Url { get; set; }
        }

        private class ExcelOptimization : IDisposable
        {
            private readonly dynamic _app;
            private readonly bool? _prevScreenUpdating, _prevDisplayAlerts, _prevEnableEvents;
            private readonly int? _prevCalculation;

            public ExcelOptimization(dynamic app)
            {
                _app = app;
                bool? su = null, da = null, ee = null; int? calc = null;
                ExcelHelper.Try(() => { su = app.ScreenUpdating; app.ScreenUpdating = false; });
                ExcelHelper.Try(() => { da = app.DisplayAlerts; app.DisplayAlerts = false; });
                ExcelHelper.Try(() => { ee = app.EnableEvents; app.EnableEvents = false; });
                ExcelHelper.Try(() => { calc = app.Calculation; app.Calculation = XlCalculationManual; });
                _prevScreenUpdating = su; _prevDisplayAlerts = da; _prevEnableEvents = ee; _prevCalculation = calc;
            }

            public void Dispose()
            {
                ExcelHelper.Try(() => _app.Calculation = _prevCalculation ?? XlCalculationAutomatic);
                ExcelHelper.Try(() => _app.EnableEvents = _prevEnableEvents ?? true);
                ExcelHelper.Try(() => _app.DisplayAlerts = _prevDisplayAlerts ?? true);
                ExcelHelper.Try(() => _app.ScreenUpdating = _prevScreenUpdating ?? true);
            }
        }
    }
}