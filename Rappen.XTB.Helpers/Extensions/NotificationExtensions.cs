using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.UserControls;

namespace Rappen.XTB.Helpers.Extensions
{
    /// <summary>
    /// Provides extension methods for showing XrmToolBox notifications from any <see cref="PluginControlBase"/>
    /// Wraps the built-in notification control with simpler helpers for the most common scenarios.
    /// Without these, you need to follow https://www.xrmtoolbox.com/documentation/for-developers/Use-Notifications/
    /// </summary>
    public static class NotificationExtensions
    {
        private sealed class NotificationActions
        {
            public Action LinkAction { get; set; }
            public Action ButtonAction { get; set; }
        }

        private static readonly Dictionary<NotificationControl, NotificationActions> ActionsByNotification =
            new Dictionary<NotificationControl, NotificationActions>();

        /// <summary>
        /// Shows an informational notification to the user.
        /// Use this for neutral status messages that do not indicate success or failure.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowInfoMessage(this PluginControlBase control, string message, int autoHideMilliseconds = 0) =>
            ShowMessage(control, message, NotificationType.Info, autoHideMilliseconds);

        /// <summary>
        /// Shows a success notification to the user.
        /// Use this after a completed action such as save, generate, or load.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowSuccessMessage(this PluginControlBase control, string message, int autoHideMilliseconds = 0) =>
            ShowMessage(control, message, NotificationType.Success, autoHideMilliseconds);

        /// <summary>
        /// Shows a warning notification to the user.
        /// Use this when the user should be made aware of a non-fatal issue or important condition.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowWarningMessage(this PluginControlBase control, string message, int autoHideMilliseconds = 0) =>
            ShowMessage(control, message, NotificationType.Warning, autoHideMilliseconds);

        /// <summary>
        /// Shows an error notification to the user.
        /// Use this for visible non-dialog error feedback when an operation could not complete as expected.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowErrorMessage(this PluginControlBase control, string message, int autoHideMilliseconds = 0) =>
            ShowMessage(control, message, NotificationType.Error, autoHideMilliseconds);

        /// <summary>
        /// Shows a notification with a clickable link action.
        /// Use this when the notification should let the user navigate or trigger a follow-up action.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="linkText">The text shown for the clickable link.</param>
        /// <param name="onClick">The action to execute when the link is clicked.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowLinkMessage(this PluginControlBase control, string message, string linkText, Action onClick, int autoHideMilliseconds = 0) =>
            ShowMessage(control, message, NotificationType.Info, autoHideMilliseconds, NotificationAction.Link, linkText, onClick);

        /// <summary>
        /// Shows a notification with a clickable button action.
        /// Use this when the user should be able to explicitly continue or open more details from the notification.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="buttonText">The text shown on the action button.</param>
        /// <param name="onClick">The action to execute when the button is clicked.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowButtonMessage(this PluginControlBase control, string message, string buttonText, Action onClick, int autoHideMilliseconds = 0) =>
            ShowMessage(control, message, NotificationType.Info, autoHideMilliseconds, NotificationAction.Button, null, null, buttonText, onClick);

        /// <summary>
        /// Shows a notification with both a clickable link and a button action.
        /// Use this when the notification should offer two different follow-up actions.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="linkText">The text shown for the clickable link.</param>
        /// <param name="onLinkClick">The action to execute when the link is clicked.</param>
        /// <param name="buttonText">The text shown on the action button.</param>
        /// <param name="onButtonClick">The action to execute when the button is clicked.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowLinkAndButtonMessage(this PluginControlBase control, string message, string linkText, Action onLinkClick, string buttonText, Action onButtonClick, int autoHideMilliseconds = 0) =>
            ShowMessage(control, message, NotificationType.Info, autoHideMilliseconds, NotificationAction.Both, linkText, onLinkClick, buttonText, onButtonClick);

        /// <summary>
        /// Shows a notification with a link that opens a local file or folder using the shell.
        /// Use this as a convenience wrapper when the follow-up action is simply to open a generated file.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="filePath">The full path of the file or folder to open.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowFileLinkMessage(this PluginControlBase control, string message, string filePath, int autoHideMilliseconds = 0) =>
            control.ShowLinkMessage(
                message,
                "Open file",
                () => Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }),
                autoHideMilliseconds);

        /// <summary>
        /// Shows a notification with a link that opens a URL using the shell.
        /// Use this as a convenience wrapper for documentation, help pages, or external resources.
        /// </summary>
        /// <param name="control">The plugin control hosting the XrmToolBox notification area.</param>
        /// <param name="message">The message text to display in the notification.</param>
        /// <param name="url">The URL to open when the link is clicked.</param>
        /// <param name="linkText">The text shown for the clickable link.</param>
        /// <param name="autoHideMilliseconds">
        /// Optional delay before the notification is hidden automatically, in milliseconds.
        /// Use <c>0</c> to keep it visible until replaced or manually closed.
        /// </param>
        public static void ShowUrlLinkMessage(this PluginControlBase control, string message, string url, string linkText = "Learn more", int autoHideMilliseconds = 0) =>
            control.ShowLinkMessage(
                message,
                linkText,
                () => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }),
                autoHideMilliseconds);

        private static void ShowMessage(
            this PluginControlBase control,
            string message,
            NotificationType type,
            int autoHideMilliseconds = 0,
            NotificationAction action = NotificationAction.None,
            string linkText = null,
            Action linkAction = null,
            string buttonText = null,
            Action buttonAction = null,
            bool canBeClosed = true)
        {
            var notification = control?.Notification;
            if (notification == null)
            {
                return;
            }

            notification.LinkClicked -= Notification_LinkClicked;
            notification.ButtonClicked -= Notification_ButtonClicked;

            ActionsByNotification.Remove(notification);

            if (action != NotificationAction.None)
            {
                ActionsByNotification[notification] = new NotificationActions
                {
                    LinkAction = linkAction,
                    ButtonAction = buttonAction
                };

                notification.LinkClicked += Notification_LinkClicked;
                notification.ButtonClicked += Notification_ButtonClicked;
            }

            notification.Message = message;
            notification.Type = type;
            notification.Action = action;
            notification.CanBeClosed = canBeClosed;
            notification.LinkText = linkText;
            notification.ButtonText = buttonText;
            notification.SetVisible(true, autoHideMilliseconds);
        }

        private static void Notification_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is NotificationControl notification &&
                ActionsByNotification.TryGetValue(notification, out var actions))
            {
                actions.LinkAction?.Invoke();
            }
        }

        private static void Notification_ButtonClicked(object sender, EventArgs e)
        {
            if (sender is NotificationControl notification &&
                ActionsByNotification.TryGetValue(notification, out var actions))
            {
                actions.ButtonAction?.Invoke();
            }
        }
    }
}