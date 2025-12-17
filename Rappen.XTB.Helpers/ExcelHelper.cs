using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XTB.Helpers.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
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

        // NOTE: Optional Excel/UI polish. Failures ignored per CONTRIBUTING.md §3 (non‑critical, documented).
        private static void Try(Action action)
        { try { action(); } catch { /* ignored: optional non-critical */ } }

        /// <summary>
        /// Exports current grid content plus FetchXML/Layout to Excel, applying formatting and link enrichment.
        /// </summary>
        public static void ExportToExcel(PluginControlBase tool, XRMDataGridView xrmgrid, bool addLinks, string fetch, string layout, Action afterExport, int? columnMaxWidth = null)
        {
            var dataObj = BuildClipboardDataOnUIThread(xrmgrid, addLinks, tool.ConnectionDetail);
            if (dataObj == null)
            {
                return;
            }

            tool.WorkAsync(new WorkAsyncInfo
            {
                Message = "Opening in Excel...",
                Work = (w, a) => ExportClipboardToExcel(w, tool.ToolName, fetch, layout, tool.ConnectionDetail, dataObj, columnMaxWidth),
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

        private static DataObject BuildClipboardDataOnUIThread(XRMDataGridView xrmgrid, bool addLinks, ConnectionDetail conndet)
        {
            if (xrmgrid == null || xrmgrid.IsDisposed)
            {
                return null;
            }

            if (!xrmgrid.IsHandleCreated)
            {
                _ = xrmgrid.Handle;
            }

            DataObject result = null;
            Exception invokeEx = null;

            void BuildData()
            {
                try
                {
                    var originalMode = xrmgrid.ClipboardCopyMode;
                    xrmgrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;

                    xrmgrid.SelectAll();
                    result = xrmgrid.GetClipboardContent() as DataObject;
                    xrmgrid.ClearSelection();
                    xrmgrid.ClipboardCopyMode = originalMode;

                    if (result != null && addLinks)
                    {
                        AddHtmlLinksToClipboard(result, xrmgrid, conndet);
                    }
                }
                catch (Exception ex)
                {
                    invokeEx = ex;
                }
            }

            if (xrmgrid.InvokeRequired)
            {
                xrmgrid.Invoke((MethodInvoker)BuildData);
            }
            else
            {
                BuildData();
            }

            if (invokeEx != null)
            {
                throw invokeEx;
            }

            return result;
        }

        private static void AddHtmlLinksToClipboard(DataObject dataObj, XRMDataGridView xrmgrid, ConnectionDetail conndet)
        {
            var visibleCols = xrmgrid.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();

            var primaryCol = GetPrimaryNameColumn(xrmgrid, visibleCols);
            var htmlFragment = BuildHtmlTable(xrmgrid, conndet, visibleCols, primaryCol);
            var cfHtml = WrapAsClipboardHtml(htmlFragment);
            Try(() => dataObj.SetData(DataFormats.Html, cfHtml));
        }

        private static DataGridViewColumn GetPrimaryNameColumn(XRMDataGridView grid, List<DataGridViewColumn> visibleCols)
        {
            if (!TryGetPrimaryNameAttribute(grid, out var primaryAttr))
            {
                return null;
            }

            var rootCols = visibleCols.Where(c => !c.Name.Split('|')[0].StartsWith("#")).ToList();
            var index = rootCols.FindIndex(c =>
            {
                var baseName = c.Name.Split('|')[0];
                return !baseName.Contains(".") &&
                       string.Equals(baseName, primaryAttr, StringComparison.OrdinalIgnoreCase);
            });

            return index >= 0 ? rootCols[index] : null;
        }

        private static void ExportClipboardToExcel(System.ComponentModel.BackgroundWorker bw, string toolname, string fetch, string layout, ConnectionDetail conndet, DataObject dataObj, int? columnMaxWidth)
        {
            if (dataObj == null)
            {
                return;
            }

            bw?.ReportProgress(10, "Generate rows for Excel...");

            Exception excelThreadException = null;

            var sta = new Thread(() =>
            {
                try
                {
                    SetClipboard(dataObj);
                    bw?.ReportProgress(20, "Starting Excel...");

                    dynamic app = CreateExcelApplication();
                    Try(() => app.Visible = false);

                    bw?.ReportProgress(30, "Populating results...");

                    using (new ExcelOptimization(app))
                    {
                        dynamic wb = app.Workbooks.Add();
                        dynamic resultSheet = wb.Worksheets[1];
                        resultSheet.Name = $"{toolname} - Result";

                        PasteAndFormatResultSheet(resultSheet, columnMaxWidth, bw);

                        bw?.ReportProgress(40, "Copying FetchXML and LayoutXML...");
                        PopulateSourceSheet(wb, toolname, fetch, layout, conndet);

                        bw?.ReportProgress(90, "Finalizing Excel...");
                        Try(() => resultSheet.Activate());
                        Try(() => resultSheet.Range["A1", "A1"].Select());
                    }

                    bw?.ReportProgress(95, "Opening Excel...");
                    Try(() => app.Visible = true);
                    Try(() => app.Workbooks[1].Activate());
                    Try(() => app.Windows[1].Activate());
                }
                catch (Exception ex)
                {
                    excelThreadException = ex;
                }
            });

            sta.SetApartmentState(ApartmentState.STA);
            sta.IsBackground = true;
            sta.Start();
            sta.Join();

            if (excelThreadException != null)
            {
                throw excelThreadException;
            }
        }

        private static void SetClipboard(DataObject dataObj)
        {
            Try(() => Clipboard.SetDataObject(dataObj, true, retryTimes: 10, retryDelay: 50));
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    if (Clipboard.ContainsData(DataFormats.Html) || Clipboard.ContainsData(DataFormats.Text))
                    {
                        break;
                    }
                }
                catch { }
                Thread.Sleep(20);
            }
        }

        private static dynamic CreateExcelApplication()
        {
            var excelType = Type.GetTypeFromProgID("Excel.Application")
                ?? throw new Exception("Microsoft Excel is not installed.");
            try
            {
                return Activator.CreateInstance(excelType);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start Microsoft Excel.{Environment.NewLine}{ex.Message}", ex);
            }
        }

        private static void PasteAndFormatResultSheet(dynamic resultSheet, int? columnMaxWidth, System.ComponentModel.BackgroundWorker bw)
        {
            try
            {
                resultSheet.Paste(resultSheet.Cells[1, 1]);
            }
            catch
            {
                Try(() => resultSheet.Paste());
            }
            Try(() => resultSheet.Application.CutCopyMode = false);

            // Format link icon column if present
            Try(() =>
            {
                var headerA1 = (string)(resultSheet.Cells[1, 1].Value ?? string.Empty);
                if (string.Equals(headerA1, LinkIcon, StringComparison.Ordinal))
                {
                    resultSheet.Columns[1].HorizontalAlignment = XlHAlignCenter;
                }
            });

            // Format header row
            dynamic header = resultSheet.Rows[1];
            header.Font.Bold = true;
            header.Borders[XlEdgeBottom].LineStyle = XlContinuous;
            header.Borders[XlEdgeBottom].Weight = XlThick;
            header.AutoFilter(1, Type.Missing, XlAnd, Type.Missing, true);
            Try(() => resultSheet.Application.ActiveWindow.SplitRow = 1);
            Try(() => resultSheet.Application.ActiveWindow.FreezePanes = true);
            Try(() => resultSheet.Rows.VerticalAlignment = XlVAlignTop);

            // AutoSize and constrain columns
            bw?.ReportProgress(35, "Autosizing columns...");
            FormatColumns(resultSheet, columnMaxWidth);
        }

        private static void FormatColumns(dynamic resultSheet, int? columnMaxWidth)
        {
            Try(() =>
            {
                dynamic used = resultSheet.UsedRange;

                // Temporarily disable wrap to get proper column AutoFit measurement
                used.WrapText = false;
                used.Columns.AutoFit();

                // Apply column max width constraint if specified
                if (columnMaxWidth.HasValue)
                {
                    var excelMaxWidth = (columnMaxWidth.Value - 5) / 7.0;
                    for (int col = 1; col <= used.Columns.Count; col++)
                    {
                        Try(() =>
                        {
                            dynamic column = resultSheet.Columns[col];
                            if (column.ColumnWidth > excelMaxWidth)
                            {
                                column.ColumnWidth = excelMaxWidth;
                            }
                        });
                    }
                }

                // Always enable WrapText to support multiline content display (both \r\n and <br/>)
                // Row height stays at default - no AutoFit on rows
                used.WrapText = true;
            });
        }

        private static void PopulateSourceSheet(dynamic wb, string toolname, string fetch, string layout, ConnectionDetail conndet)
        {
            dynamic sourceSheet = wb.Sheets.Add();
            Try(() => sourceSheet.Move(After: wb.Sheets[wb.Sheets.Count]));
            sourceSheet.Name = $"{toolname} - Source";

            var fetchLocal = RemovePagingFromFetch(fetch);

            sourceSheet.Cells[1, 1].Value = "Connection";
            sourceSheet.Cells[1, 2].Value = conndet?.ConnectionName;
            sourceSheet.Cells[2, 1].Value = "URL";
            sourceSheet.Cells[2, 2].Value = conndet?.WebApplicationUrl;
            sourceSheet.Cells[3, 1].Value = "Query";
            sourceSheet.Cells[3, 2].Value = fetchLocal;
            if (!string.IsNullOrEmpty(layout))
            {
                sourceSheet.Cells[4, 1].Value = "Layout";
                sourceSheet.Cells[4, 2].Value = layout;
            }

            dynamic sourceHeaderCol = sourceSheet.Columns[1];
            sourceHeaderCol.Font.Bold = true;
            sourceHeaderCol.Cells.VerticalAlignment = XlVAlignTop;

            Try(() => sourceSheet.Columns[1].AutoFit());
            Try(() => sourceSheet.Columns[2].WrapText = true);
            Try(() =>
            {
                const int targetPixels = 800;
                var excelWidth = (targetPixels - 5) / 7.0;
                sourceSheet.Columns[2].ColumnWidth = excelWidth;
            });
            Try(() => sourceSheet.Rows.VerticalAlignment = XlVAlignTop);
            Try(() => sourceSheet.Rows.AutoFit());
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

        private static string BuildHtmlTable(XRMDataGridView grid, ConnectionDetail conndet, List<DataGridViewColumn> visibleCols, DataGridViewColumn primaryCol)
        {
            var hasPrimary = primaryCol != null;
            var primaryAttr = hasPrimary ? primaryCol.Name.Split('|')[0] : null;

            var sb = new StringBuilder();
            sb.Append("<table border=\"0\" cellpadding=\"2\" cellspacing=\"0\"><tr>");

            if (!hasPrimary)
            {
                sb.Append($"<th>{HtmlEncode(LinkIcon)}</th>");
            }

            foreach (var col in visibleCols)
            {
                sb.Append($"<th>{HtmlEncode(col.HeaderText)}</th>");
            }

            sb.Append("</tr>");

            for (var r = 0; r < grid.Rows.Count; r++)
            {
                if (!grid.Rows[r].Visible)
                {
                    continue;
                }

                AppendHtmlRow(sb, grid, r, visibleCols, conndet, hasPrimary, primaryAttr);
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        private static void AppendHtmlRow(StringBuilder sb, XRMDataGridView grid, int rowIndex, List<DataGridViewColumn> visibleCols, ConnectionDetail conndet, bool hasPrimary, string primaryAttr)
        {
            var entity = grid.GetXRMEntity(rowIndex);
            var urlPrimary = entity?.GetEntityUrl(conndet);

            sb.Append("<tr>");

            if (!hasPrimary)
            {
                sb.Append(string.IsNullOrWhiteSpace(urlPrimary)
                    ? "<td></td>"
                    : $"<td><a href=\"{HtmlAttr(urlPrimary)}\">{HtmlEncode(LinkIcon)}</a></td>");
            }

            foreach (var col in visibleCols)
            {
                AppendHtmlCell(sb, grid, rowIndex, col, entity, conndet, hasPrimary, primaryAttr, urlPrimary);
            }

            sb.Append("</tr>");
        }

        private static void AppendHtmlCell(StringBuilder sb, XRMDataGridView grid, int rowIndex, DataGridViewColumn col, Entity entity, ConnectionDetail conndet, bool hasPrimary, string primaryAttr, string urlPrimary)
        {
            var raw = grid[col.Index, rowIndex]?.Value?.ToString();
            var display = raw?.Replace("\r\n", "\n").Replace("\n", "<br/>");
            var encoded = HtmlEncode(display);
            var baseName = col.Name.Split('|')[0];
            var isRootAttr = !baseName.StartsWith("#") && !baseName.Contains(".");

            string linkUrl = null;

            if (hasPrimary && isRootAttr && string.Equals(baseName, primaryAttr, StringComparison.OrdinalIgnoreCase))
            {
                linkUrl = urlPrimary;
            }
            else if (isRootAttr && entity != null && entity.Contains(baseName))
            {
                var val = entity[baseName];
                if (val is AliasedValue av)
                {
                    val = av.Value;
                }

                if (val is EntityReference er)
                {
                    linkUrl = er.GetEntityUrl(conndet);
                }
            }

            sb.Append(!string.IsNullOrWhiteSpace(linkUrl) && !string.IsNullOrEmpty(display)
                ? $"<td><a href=\"{HtmlAttr(linkUrl)}\">{encoded}</a></td>"
                : $"<td>{encoded}</td>");
        }

        private static string WrapAsClipboardHtml(string fragment)
        {
            const string headerTemplate =
                "Version:1.0\r\nStartHTML:{0:0000000000}\r\nEndHTML:{1:0000000000}\r\nStartFragment:{2:0000000000}\r\nEndFragment:{3:0000000000}\r\n";

            var pre = "<html><body><!--StartFragment-->";
            var post = "<!--EndFragment--></body></html>";
            var html = pre + fragment + post;

            var utf8 = Encoding.UTF8;
            var headerLenBytes = Encoding.ASCII.GetByteCount(string.Format(headerTemplate, 0, 0, 0, 0));

            var startHTML = headerLenBytes;
            var endHTML = headerLenBytes + utf8.GetByteCount(html);
            var startFragment = headerLenBytes + utf8.GetByteCount(pre);
            var endFragment = startFragment + utf8.GetByteCount(fragment);

            var header = string.Format(headerTemplate, startHTML, endHTML, startFragment, endFragment);
            return header + html;
        }

        private static string HtmlEncode(string s) =>
            string.IsNullOrEmpty(s) ? string.Empty :
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        private static string HtmlAttr(string s) =>
            string.IsNullOrEmpty(s) ? string.Empty :
            s.Replace("&", "&amp;").Replace("\"", "&quot;");

        private static bool TryGetPrimaryNameAttribute(XRMDataGridView grid, out string primaryName)
        {
            primaryName = null;
            if (grid?.Service == null || string.IsNullOrWhiteSpace(grid.EntityName))
            {
                return false;
            }

            if (!PrimaryNameCache.TryGetValue(grid.EntityName, out primaryName))
            {
                var meta = grid.Service.GetEntity(grid.EntityName);
                primaryName = meta?.PrimaryNameAttribute;
                if (string.IsNullOrWhiteSpace(primaryName))
                {
                    return false;
                }

                PrimaryNameCache[grid.EntityName] = primaryName;
            }
            return true;
        }

        // Helper class to manage Excel optimization settings
        private class ExcelOptimization : IDisposable
        {
            private readonly dynamic _app;
            private readonly bool? _prevScreenUpdating;
            private readonly bool? _prevDisplayAlerts;
            private readonly bool? _prevEnableEvents;
            private readonly int? _prevCalculation;

            public ExcelOptimization(dynamic app)
            {
                _app = app;

                bool? screenUpdating = null;
                bool? displayAlerts = null;
                bool? enableEvents = null;
                int? calculation = null;

                ExcelHelper.Try(() => { screenUpdating = app.ScreenUpdating; app.ScreenUpdating = false; });
                ExcelHelper.Try(() => { displayAlerts = app.DisplayAlerts; app.DisplayAlerts = false; });
                ExcelHelper.Try(() => { enableEvents = app.EnableEvents; app.EnableEvents = false; });
                ExcelHelper.Try(() => { calculation = app.Calculation; app.Calculation = XlCalculationManual; });

                _prevScreenUpdating = screenUpdating;
                _prevDisplayAlerts = displayAlerts;
                _prevEnableEvents = enableEvents;
                _prevCalculation = calculation;
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