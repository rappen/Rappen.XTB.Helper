using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public class XRMLookupDialog : Component
    {
        #region Private Fields

        private string[] logicalNames;

        private string defaultEntity;
        private Guid defaultViewId;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Selected records.
        /// </summary>
        [Browsable(false)]
        public Entity[] Records { get; private set; }

        /// <summary>
        /// Selected record, or first selected record is Multiselect=true.
        /// </summary>
        [Browsable(false)]
        public Entity Record
        {
            get
            {
                return Records?.FirstOrDefault();
            }
            set
            {
                SetRecord(value);
            }
        }

        [Description("Entity logicalname to select records from for standard lookups.")]
        public string LogicalName
        {
            get => logicalNames?.Length == 1 ? logicalNames[0] : string.Empty;
            set
            {
                logicalNames = new string[] { value };
            }
        }

        public Dictionary<string, List<Entity>> AdditionalViews { get; set; } = new Dictionary<string, List<Entity>>();

        [Description("List of entity logicalnames that shall be available to select from for polymorphic lookups.")]
        public string[] LogicalNames
        {
            get => logicalNames;
            set
            {
                if (value != null && value.Length > 0)
                {
                    // Clever way to allow comma separated logicalnames too in the array
                    logicalNames =
                        string.Join(",", value)
                        .Split(',')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToArray();
                }
                else
                {
                    logicalNames = null;
                }
            }
        }

        [DefaultValue(false)]
        [Description("Controls whether multiple records can be selected in the dialog.")]
        public bool Multiselect { get; set; } = false;

        [DefaultValue(false)]
        [Description("Include personal views for record selection.")]
        public bool IncludePersonalViews { get; set; } = false;

        [DefaultValue(true)]
        [Description("True to show friendly names, False to show logical names and guid etc.")]
        public bool ShowFriendlyNames { get; set; } = true;

        [DefaultValue(true)]
        [Description("Determines if the button Remove Value should be shown.")]
        public bool ShowRemoveButton { get; set; } = true;

        [DefaultValue(false)]
        [Description("After the selection and closed the dialog, the record(s) will be reloaded with all attributes.")]
        public bool ReloadWithAllAttributesAfterSelected { get; set; } = false;

        /// <summary>
        /// IOrganizationService to use when retrieving metadata, views and records.
        /// </summary>
        [Browsable(false)]
        public IOrganizationService Service { get; set; }

        [Description("The string to display in the title bar of the dialog box.")]
        public string Title { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>Shows the form as a modal dialog box.</summary>
        /// <returns>One of the System.Windows.Forms.DialogResult values.</returns>
        /// <exception cref="T:System.InvalidOperationException">The form being shown is already visible.-or- The form being shown is disabled.-or-
        ///     The form being shown is not a top-level window.-or- The form being shown as a
        ///     dialog box is already a modal form.-or-The current process is not running in
        ///     user interactive mode (for more information, see System.Windows.Forms.SystemInformation.UserInteractive).</exception>
        public DialogResult ShowDialog()
        {
            return this.ShowDialog(null);
        }

        /// <summary>Shows the form as a modal dialog box with the specified owner.</summary>
        /// <param name="owner">Any object that implements System.Windows.Forms.IWin32Window that represents the top-level window that will own the modal dialog box.</param>
        /// <returns>One of the System.Windows.Forms.DialogResult values.</returns>
        /// <exception cref="T:System.Exception">This exception can be thrown if Service property is not set.</exception>
        /// <exception cref="T:System.ArgumentException">The form specified in the owner parameter is the same as the form being shown.</exception>
        /// <exception cref="T:System.InvalidOperationException">The form being shown is already visible.-or- The form being shown is disabled.-or-
        ///     The form being shown is not a top-level window.-or- The form being shown as a
        ///     dialog box is already a modal form.-or-The current process is not running in
        ///     user interactive mode (for more information, see System.Windows.Forms.SystemInformation.UserInteractive).</exception>
        public DialogResult ShowDialog(IWin32Window owner)
        {
            if (Service == null)
            {
                throw new Exception("Service property must be set before calling ShowDialog.");
            }
            if (logicalNames == null || logicalNames.Length < 1)
            {
                throw new Exception("LogicalNames property must contain at least one entity before calling ShowDialog.");
            }
            var title = string.IsNullOrEmpty(Title) ? Multiselect ? "Select Records" : "Select Record" : Title;
            using (var form = new XRMLookupDialogForm(Service, LogicalNames, Multiselect, ShowFriendlyNames, IncludePersonalViews, ShowRemoveButton, title))
            {
                if (AdditionalViews.Keys.Count > 0)
                {
                    foreach (var logicalname in AdditionalViews.Keys)
                    {
                        if (!LogicalNames.Contains(logicalname)) continue;

                        foreach (var view in AdditionalViews[logicalname])
                        {
                            form.AddCustomView(logicalname, view, logicalname == defaultEntity && view.Id.Equals(defaultViewId));
                        }
                    }
                }

                var result = form.ShowDialog(owner);
                Records = form.GetSelectedRecords();
                if (ReloadWithAllAttributesAfterSelected)
                {
                    var reloadedrecords = Records.Select(r => Service.Retrieve(r.LogicalName, r.Id, new ColumnSet(true)));
                    Records = reloadedrecords.ToArray();
                }
                return result;
            }
        }

        public void SetDefaultView(string logicalName, Guid savedqueryId)
        {
            defaultEntity = logicalName;
            defaultViewId = savedqueryId;
        }

        #endregion Public Methods

        #region Private Methods

        private void SetRecord(Entity value)
        {
        }

        #endregion Private Methods
    }
}
