using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.ControlItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Controls
{
    public partial class XRMMetaAutoCompleteComboBox : ComboBox
    {
        private List<object> originalItems;
        private string search = "";

        public XRMMetaAutoCompleteComboBox()
        {
            InitializeComponent();
            TextUpdate += _TextUpdate;
            throw new NotImplementedException("I'm just trying, but definitely not ready. Maybe, in the future, we'll see... /Jonas");
        }

        private void _TextUpdate(object sender, EventArgs e)
        {
            //DroppedDown = false;
            if (originalItems == null)
            {
                originalItems = Items.Cast<object>().ToList();
            }

            Items.Cast<object>().Reverse().ToList().ForEach(i => Items.Remove(i));
            //while (Items.Count > 0)
            //{
            //    Items.RemoveAt(Items.Count-1);
            //}

            if (string.IsNullOrEmpty(Text))
            {
                Items.AddRange(originalItems.ToArray());
            }
            else
            {
                var searching = Text.ToLowerInvariant();
                Items.AddRange(originalItems.Cast<object>()
                    .Where(i => i is EntityMetadata)
                    .Cast<EntityMetadata>()
                    .Where(m =>
                        m.LogicalName?.ToLowerInvariant()?.Contains(searching) == true ||
                        m.DisplayName?.UserLocalizedLabel?.Label?.ToLowerInvariant()?.Contains(searching) == true
                     ).ToArray());

                Items.AddRange(originalItems.Cast<object>()
                    .Where(i => i is EntityMetadataItem)
                    .Cast<EntityMetadataItem>()
                    .Where(m =>
                        m.Metadata?.LogicalName?.ToLowerInvariant()?.Contains(searching) == true ||
                        m.Metadata?.DisplayName?.UserLocalizedLabel?.Label?.ToLowerInvariant()?.Contains(searching) == true
                    ).ToArray());

                Items.AddRange(originalItems.Cast<object>()
                    .Where(i => i is AttributeMetadataItem)
                    .Cast<AttributeMetadataItem>()
                    .Where(m =>
                        m.Metadata?.LogicalName?.ToLowerInvariant()?.Contains(searching) == true ||
                        m.Metadata?.DisplayName?.UserLocalizedLabel?.Label?.ToLowerInvariant()?.Contains(searching) == true
                    ).ToArray());
            }
            //DroppedDown = true;
        }
    }
}