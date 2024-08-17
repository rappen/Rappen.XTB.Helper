﻿namespace Rappen.XTB.Helpers
{
    using Microsoft.Xrm.Sdk.Metadata;
    using Rappen.XRM.Helpers.Extensions;
    using Rappen.XRM.Helpers.Interfaces;
    using Rappen.XTB.Helpers.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using System.Xml;

    public class ControlUtils
    {
        public static bool GetControlDefinition(Control control, out string attribute, out bool required, out string defaultvalue)
        {
            var tags = control.Tag != null ? control.Tag.ToString().Split('|') : new string[] { };
            attribute = tags.Length > 0 ? tags[0] : "";
            required = tags.Length > 1 && bool.Parse(tags[1]);
            defaultvalue = tags.Length > 2 ? tags[2] : control is CheckBox ? "false" : "";
            return !string.IsNullOrWhiteSpace(attribute);
        }

        public static string ControlsChecksum(Control.ControlCollection controls)
        {
            if (controls?.Count == 0)
            {
                return string.Empty;
            }
            var result = string.Join("|", controls.OfType<Control>().OrderBy(c => c.TabIndex).Select(c => GetValueFromControl(c) + "|" + ControlsChecksum(c.Controls)));
            while (result.Contains("||"))
            {
                result = result.Replace("||", "|");
            }
            return result;
        }

        public static string GetValueFromControl(Control control)
        {
            var result = "";
            if (control is CheckBox cb)
            {
                result = cb.Checked ? "true" : "false";
            }
            else if (control is TextBox tx)
            {
                result = tx.Text;
            }
            else if (control is ComboBox cb2)
            {
                var item = cb2.SelectedItem;
                if (item is IXRMControlItem ci)
                {
                    result = ci.GetValue();
                }
                else
                {
                    result = cb2.Text;
                }
            }
            return result;
        }

        public static Dictionary<string, string> GetAttributesCollection(Control.ControlCollection controls, bool validate = false)
        {
            if (controls?.Count == 0)
            {
                return null;
            }
            var collection = new Dictionary<string, string>();

            foreach (Control control in controls.OfType<Control>().OrderBy(y => y.TabIndex))
            {
                if (control.Tag != null)
                {
                    if (GetControlDefinition(control, out string attribute, out bool required, out string defaultvalue))
                    {
                        var value = GetValueFromControl(control);
                        if (validate && required && string.IsNullOrEmpty(value))
                        {
                            throw new ArgumentNullException(attribute, "Field cannot be empty");
                        }
                        if (required || value != defaultvalue)
                        {
                            collection.Add(attribute, value);
                        }
                    }
                }
                if (GetAttributesCollection(control.Controls, validate) is Dictionary<string, string> children)
                {
                    foreach (var child in children)
                    {
                        collection.Add(child.Key, child.Value);
                    }
                }
            }
            return collection;
        }

        public static void FillControls(Dictionary<string, string> collection, Control.ControlCollection controls, IDefinitionSavable saveable)
        {
            if (controls?.Count == 0)
            {
                return;
            }
            controls.OfType<Control>().Where(y => y.Tag != null).OrderBy(y => y.TabIndex).ToList().ForEach(c => FillControl(collection, c, saveable));
            controls.OfType<Panel>().OrderBy(p => p.TabIndex).ToList().ForEach(p => FillControls(collection, p.Controls, saveable));
            controls.OfType<GroupBox>().OrderBy(g => g.TabIndex).ToList().ForEach(g => FillControls(collection, g.Controls, saveable));
        }

        public static string GetLayoutXMLFromCells(EntityMetadata entitymeta, Dictionary<string, int> cells)
        {
            if (entitymeta == null || cells == null)
            {
                return string.Empty;
            }
            return $@"<grid name='resultset' object='{entitymeta.ObjectTypeCode}' jump='{entitymeta.PrimaryNameAttribute}' select='1' icon='1' preview='1'>
  <row name='result' id='
            {entitymeta.PrimaryIdAttribute}'>
    {string.Join("\n    ", cells.Select(c => $"<cell name='{c.Key}' width='{c.Value}'/>"))}
  </row>
</grid>";
        }
        public static Dictionary<string, int> GetCellsFromLayoutXML(string layoutxml)
        {
            string GetCellName(XmlNode node)
            {
                if (node != null && node.Attributes != null && node.Attributes["name"] is XmlAttribute attr)
                {
                    return attr.Value;
                }
                return string.Empty;
            }
            int GetCellWidth(XmlNode node)
            {
                if (node != null && node.Attributes != null)
                {
                    if (node.Attributes["ishidden"] is XmlAttribute attrhidden &&
                        attrhidden.Value is string hidden)
                    {
                        hidden = hidden.ToLowerInvariant().Trim();
                        return hidden == "1" || hidden == "true" ? 0 : 100;
                    }
                    if (node.Attributes["width"] is XmlAttribute attrwidth &&
                        int.TryParse(attrwidth.Value, out var width))
                    {
                        return width;
                    }
                }
                return 100;
            }

            if (!string.IsNullOrEmpty(layoutxml) && layoutxml.ToXml().SelectSingleNode("grid") is XmlElement grid)
            {
                var cells = grid.SelectSingleNode("row")?
                    .ChildNodes.Cast<XmlNode>()
                    .Where(n => n.Name == "cell")
                    .Select(c => new KeyValuePair<string, int>(GetCellName(c), GetCellWidth(c)))
                    .ToDictionary(c => c.Key, c => c.Value);
                return cells?.Count > 0 ? cells : null;
            }
            return null;
        }

        private class TextBoxEventHandler
        {
            private readonly TextBox txt;
            private readonly IDefinitionSavable saveable;
            private bool attachedValidated;

            public TextBoxEventHandler(TextBox txt, IDefinitionSavable saveable)
            {
                this.txt = txt;
                this.saveable = saveable;
            }

            public void Attach()
            {
                txt.TextChanged += OnTextChanged;
            }

            private void OnTextChanged(object sender, EventArgs e)
            {
                saveable.Save(true);

                if (!attachedValidated)
                {
                    txt.Validated += OnValidated;
                    attachedValidated = true;
                }
            }

            private void OnValidated(object sender, EventArgs e)
            {
                saveable.Save(false);
                txt.Validated -= OnValidated;
                attachedValidated = false;
            }
        }

        private class ComboBoxEventHandler
        {
            private readonly ComboBox cmb;
            private readonly IDefinitionSavable saveable;
            private bool attachedValidated;

            public ComboBoxEventHandler(ComboBox cmb, IDefinitionSavable saveable)
            {
                this.cmb = cmb;
                this.saveable = saveable;
            }

            public void Attach()
            {
                cmb.TextChanged += OnTextChanged;
                cmb.SelectedIndexChanged += OnSelectedIndexChanged;
            }

            private void OnSelectedIndexChanged(object sender, EventArgs e)
            {
                saveable.Save(false);
                cmb.Validated -= OnValidated;
                attachedValidated = false;
            }

            private void OnTextChanged(object sender, EventArgs e)
            {
                if (cmb.SelectedIndex != -1)
                {
                    return;
                }

                saveable.Save(true);

                if (!attachedValidated)
                {
                    cmb.Validated += OnValidated;
                    attachedValidated = true;
                }
            }

            private void OnValidated(object sender, EventArgs e)
            {
                saveable.Save(false);
                cmb.Validated -= OnValidated;
                attachedValidated = false;
            }
        }

        public static void FillControl(Dictionary<string, string> collection, Control control, IDefinitionSavable saveable)
        {
            if (control.Tag != null && control.Tag.ToString() != "uiname" && GetControlDefinition(control, out string attribute, out bool required, out string defaultvalue))
            {
                if (!collection.TryGetValue(attribute, out string value))
                {
                    value = defaultvalue;
                }
                if (control is CheckBox chkbox)
                {
                    bool.TryParse(value, out bool chk);
                    chkbox.Checked = chk;
                    if (saveable != null)
                    {
                        chkbox.CheckedChanged += (s, e) => saveable.Save(false);
                    }
                }
                else if (control is TextBox txt)
                {
                    txt.Text = value;
                    if (saveable != null)
                    {
                        new TextBoxEventHandler(txt, saveable).Attach();
                    }
                }
                else if (control is ComboBox cmb)
                {
                    SetComboBoxValue(cmb, value, saveable);
                }
            }
        }

        public static void SetComboBoxValue(ComboBox cmb, string value, IDefinitionSavable saveable = null)
        {
            object selitem = null;
            foreach (var item in cmb.Items)
            {
                if (item is IXRMControlItem xi && xi.GetValue() == value)
                {
                    selitem = item;
                    break;
                }
            }
            if (selitem != null)
            {
                cmb.SelectedItem = selitem;
            }
            else if (value != null && cmb.Items.IndexOf(value) >= 0)
            {
                cmb.SelectedItem = value;
            }
            else
            {
                cmb.Text = value;
            }
            if (saveable != null)
            {
                new ComboBoxEventHandler(cmb, saveable).Attach();
            }
        }

        public static DialogResult PromptDialog(string text, string caption, bool multi, ref string value)
        {
            Form prompt = new Form();
            prompt.Width = 500;
            prompt.Height = multi ? 250 : 150;
            prompt.Text = caption;
            prompt.StartPosition = FormStartPosition.CenterScreen;
            prompt.FormBorderStyle = multi ? FormBorderStyle.SizableToolWindow : FormBorderStyle.FixedToolWindow;
            Label textLabel = new Label()
            {
                Left = 50,
                Top = 20,
                Width = 430,
                Text = text
            };
            TextBox textBox = new TextBox()
            {
                Left = 50,
                Top = 45,
                Width = 400,
                Height = multi ? 120 : 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Multiline = multi,
                AcceptsReturn = multi,
                Text = value
            };
            Button cancellation = new Button()
            {
                Text = "Cancel",
                Left = 220,
                Width = 100,
                Top = multi ? 180 : 80,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel
            };
            Button confirmation = new Button()
            {
                Text = "OK",
                Left = 350,
                Width = 100,
                Top = multi ? 180 : 80,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };
            //confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(cancellation);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.CancelButton = cancellation;
            prompt.AcceptButton = confirmation;
            var result = prompt.ShowDialog();
            if (result == DialogResult.OK)
            {
                value = textBox.Text;
            }
            return result;
        }
    }
}