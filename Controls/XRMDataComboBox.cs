using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers.ControlItems;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public delegate void ProgressUpdate(string message);
    public delegate void RetrieveComplete(int itemCount, Entity FirstItem);

    public partial class XRMDataComboBox : ComboBox
    {
        #region Private properties
        private string displayFormat = string.Empty;
        private IEnumerable<Entity> entities;
        private IOrganizationService organizationService;
        #endregion

        #region Public Constructors

        public XRMDataComboBox()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

        [Category("Rappen XRM")]
        [Description("Indicates the source of data (EntityCollection) for the XRMDataComboBox control.")]
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
                IEnumerable<Entity> newEntities = null;
                if (value is EntityCollection entityCollection)
                {
                    newEntities = entityCollection.Entities;
                }
                else if (value is IEnumerable<Entity> entities)
                {
                    newEntities = entities;
                }
                if (newEntities != null)
                {
                    entities = newEntities;
                    Refresh();
                }
            }
        }

        [Category("Rappen XRM")]
        [DisplayName("Display Format")]
        [Description("Single attribute from datasource to display for items, or use XRM Tokens syntax freely https://jonasr.app/xrm-tokens/")]
        public string DisplayFormat
        {
            get { return displayFormat; }
            set
            {
                if (value != displayFormat)
                {
                    displayFormat = value;
                    Refresh();
                }
            }
        }

        [Browsable(false)]
        public IOrganizationService Service
        {
            get { return organizationService; }
            set
            {
                organizationService = value;
                Refresh();
            }
        }

        // Sorted not supported for databound combobox
        [Browsable(false)]
        public new bool Sorted { get; } = false;

        [Browsable(false)]
        public Entity SelectedEntity => (SelectedItem is EntityItem item) ? item.Entity : null;

        #endregion Public Properties

        #region Public Methods

        public override void Refresh()
        {
            SuspendLayout();
            var selected = SelectedEntity;
            var ds = entities?.Select(e => new EntityItem(e, displayFormat, organizationService)).ToArray();
            base.DataSource = ds;
            base.Refresh();
            if (selected != null && ds.FirstOrDefault(e => e.Entity.Id.Equals(selected.Id)) is EntityItem newselected)
            {
                SelectedItem = newselected;
            }
            ResumeLayout();
        }

        public void RetrieveMultiple(QueryBase query, ProgressUpdate progressCallback, RetrieveComplete completeCallback)
        {
            if (this.Service == null)
            {
                throw new InvalidOperationException("The Service reference must be set before calling RetrieveMultiple.");
            }

            try
            {
                var worker = new BackgroundWorker();
                worker.DoWork += (w, e) =>
                {
                    var queryExp = e.Argument as QueryBase;

                    BeginInvoke(progressCallback, "Begin Retrieve Multiple");

                    var fetchReq = new RetrieveMultipleRequest
                    {
                        Query = queryExp
                    };

                    var records = Service.RetrieveMultiple(query);

                    BeginInvoke(progressCallback, "End Retrieve Multiple");

                    e.Result = records;
                };

                worker.RunWorkerCompleted += (s, e) =>
                {
                    var records = e.Result as EntityCollection;

                    BeginInvoke(progressCallback, $"Retrieve Multiple - records returned: {records.Entities.Count}");

                    DataSource = records;

                    // make the final callback
                    BeginInvoke(completeCallback, entities?.Count(), SelectedEntity);
                };

                // kick off the worker thread!
                worker.RunWorkerAsync(query);
            }
            catch (System.ServiceModel.FaultException ex)
            {
            }
        }

        public void RetrieveMultiple(string fetchXml, ProgressUpdate progressCallback, RetrieveComplete completeCallback)
        {
            RetrieveMultiple(new FetchExpression(fetchXml), progressCallback, completeCallback);
        }

        #endregion Public Methods
    }
}
