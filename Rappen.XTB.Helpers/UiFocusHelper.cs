using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using WeifenLuo.WinFormsUI.Docking;

namespace Rappen.XTB.Helpers
{
    /// <summary>
    /// Utilities for XrmToolBox plugins to control focus/visibility of the main window and determine active state.
    /// </summary>
    public static class UiFocusHelper
    {
        #region Win32 interop

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        #endregion Win32 interop

        #region Constants

        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        #endregion Constants

        #region Public API

        /// <summary>
        /// Brings the hosting form to foreground and activates the DockContent hosting the plugin. Focuses inner control if provided.
        /// </summary>
        public static bool BringToolToFront(this PluginControlBase toolControl, Control innerToFocus = null, bool flashTopMost = true)
        {
            if (toolControl == null)
            {
                return false;
            }

            if (toolControl.InvokeRequired)
            {
                toolControl.BeginInvoke(new Action(() => BringToolToFront(toolControl, innerToFocus, flashTopMost)));
                return true;
            }

            var form = GetHostForm(toolControl);
            if (form == null)
            {
                return false;
            }

            var handle = form.Handle;

            if (IsIconic(handle))
            {
                ShowWindowAsync(handle, SW_RESTORE);
            }
            else if (!IsWindowVisible(handle) || !form.Visible)
            {
                ShowWindowAsync(handle, SW_SHOW);
                form.Show();
            }

            var dockContent = GetHostingDockContent(toolControl);
            var dockPanel = dockContent?.DockPanel ?? GetHostingDockPanel(toolControl);
            if (dockContent != null && dockPanel != null)
            {
                // Ensure visible & activated (Show is safe even if already visible)
                dockContent.Show(dockPanel);
                dockContent.Activate();
            }

            if (toolControl.Parent is TabPage tabPage &&
                tabPage.Parent is TabControl tabControl &&
                tabControl.SelectedTab != tabPage)
            {
                tabControl.SelectedTab = tabPage;
            }

            if (flashTopMost)
            {
                var wasTopMost = form.TopMost;
                try
                {
                    form.TopMost = true;
                    form.TopMost = wasTopMost;
                }
                catch
                {
                }
            }

            form.Activate();
            SetForegroundWindow(handle);

            if (innerToFocus != null && innerToFocus.CanFocus)
            {
                innerToFocus.Focus();
            }

            return true;
        }

        /// <summary>
        /// Returns true only if the plugin is currently shown and active:
        /// - Host window visible (not minimized)
        /// - App foreground
        /// - Its DockContent is the active document/content (or has focus)
        /// - (Optional future: tab/MDI checks, currently omitted for DockPanelSuite scenario)
        /// </summary>
        public static bool IsShownAndActive(this PluginControlBase toolControl)
        {
            if (toolControl == null)
            {
                return false;
            }

            try
            {
                var form = GetHostForm(toolControl);
                if (form == null)
                {
                    return false;
                }

                var handle = form.Handle;

                if (IsIconic(handle))
                {
                    return false;
                }

                if (!IsWindowVisible(handle) || !form.Visible)
                {
                    return false;
                }

                if (!IsApplicationInForeground())
                {
                    return false;
                }

                if (!IsDockContentActive(toolControl))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Public API

        #region Private helpers

        private static Form GetHostForm(PluginControlBase toolControl)
        {
            return toolControl?.TopLevelControl as Form ?? toolControl?.FindForm();
        }

        private static bool IsApplicationInForeground()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return false;
                }
                GetWindowThreadProcessId(hwnd, out var pid);
                return pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Recursively finds the first ancestor of type T in the parent chain.
        /// </summary>
        private static T FindAncestor<T>(Control start) where T : Control
        {
            Control c = start;
            while (c != null)
            {
                if (c is T match)
                {
                    return match;
                }
                c = c.Parent;
            }
            return null;
        }

        private static DockContent GetHostingDockContent(PluginControlBase toolControl)
        {
            return FindAncestor<DockContent>(toolControl);
        }

        private static DockPanel GetHostingDockPanel(PluginControlBase toolControl)
        {
            return FindAncestor<DockPanel>(toolControl);
        }

        /// <summary>
        /// Determines if the DockContent hosting this tool is the active one.
        /// </summary>
        private static bool IsDockContentActive(PluginControlBase toolControl)
        {
            var dockContent = GetHostingDockContent(toolControl);
            if (dockContent == null)
            {
                // Not dock-hosted: consider active (legacy scenario)
                return true;
            }

            var dockPanel = dockContent.DockPanel ?? GetHostingDockPanel(toolControl);
            if (dockPanel == null)
            {
                // No dock panel reference: rely on visibility & focus
                return dockContent.Visible && !dockContent.IsHidden && dockContent.ContainsFocus;
            }

            if (ReferenceEquals(dockPanel.ActiveDocument, dockContent))
            {
                return true;
            }

            if (ReferenceEquals(dockPanel.ActiveContent, dockContent))
            {
                return true;
            }

            if (dockContent.Pane != null && ReferenceEquals(dockContent.Pane.ActiveContent, dockContent))
            {
                return true;
            }

            if (dockContent.ContainsFocus)
            {
                return true;
            }

            return false;
        }

        #endregion Private helpers
    }
}