/*
 *  RappPluginControlBase.cs
 *
 *  Base class for all Rappen tools plugins for XrmToolBox.
 *
 *  In the PluginDescription.cs use like this in the GetControl() method:
 *
    public override IXrmToolBoxPluginControl GetControl()
    {
        var tool = new PluginTraceViewer();
        tool.SaveImageFromBase64(
            tool.IconBigPath,
            GetType()
            .GetCustomAttributes(typeof(ExportMetadataAttribute), false)
            .OfType<ExportMetadataAttribute>()
            .FirstOrDefault(a => a.Name == "BigImageBase64")?.Value as string);
        tool.SaveImageFromBase64(
            tool.IconSmallPath,
            GetType()
            .GetCustomAttributes(typeof(ExportMetadataAttribute), false)
            .OfType<ExportMetadataAttribute>()
            .FirstOrDefault(a => a.Name == "SmallImageBase64")?.Value as string);
        return tool;
    }
 */
using Microsoft.Toolkit.Uwp.Notifications;
using Rappen.XRM.Helpers.Extensions;
using System;
using System.IO;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Rappen.XTB.Helpers.RappXTB
{
    public abstract class RappPluginControlBase : PluginControlBase, IGitHubPlugin, IPayPalPlugin
    {
        #region Private Fields

        private const string aiEndpoint = "https://dc.services.visualstudio.com/v2/track";
        private const string aiKey1 = "eed73022-2444-45fd-928b-5eebd8fa46a6";    // jonas@rappen.net XrmToolBox
        private const string aiKey2 = "d46e9c12-ee8b-4b28-9643-dae62ae7d3d4";    // jonas@jonasr.app XrmToolBoxTools
        private AppInsights aiOld;
        private AppInsights aiNew;

        #endregion Private Fields

        #region Public Properties

        public AppInsights AppInsights => aiNew;
        public string IconSmallPath => Path.Combine(Paths.PluginsPath, "Icons", $"{Name}IconSmall.png");
        public string IconBigPath => Path.Combine(Paths.PluginsPath, "Icons", $"{Name}IconBig.png");

        #endregion Public Properties

        public abstract bool HandleToastActivationInternal(string action, string sender, ToastArguments args);

        #region Interface Properties

        public string UserName => "rappen";
        public string RepositoryName => GetRepoName();
        public string EmailAccount => "jonas@rappen.net";
        public string DonationDescription => $"{ToolName} Fan Club";

        #endregion Interface Properties

        #region Constructor

        public RappPluginControlBase()
        {
            var cls = GetType();
            UrlUtils.TOOL_NAME = cls.Name;
        }

        #endregion Constructor

        internal static string Acronym(string toolname) => toolname switch
        {
            // Tools that don't have three upper cases
            "FetchXML Builder" => "FXB",
            "UML Diagram Generator" => "UML",
            "XrmToolBox Integration Tester" => "XIT",
            "Portal Entity Permission Manager" => "EPM",
            "XRM Tokens Runner" => "XTR",
            "Shuffle Builder" => "ShB",
            "Shuffle Runner" => "ShR",
            "Shuffle Deployer" => "ShD",
            // Tools that have three upper cases
            _ => toolname.ToAcronym(3, includeAllWordInitials: true)
            //string.Join(string.Empty, Regex.Matches(name, @"((?<=^|\s)(\w{1})|([A-Z]))").OfType<Match>().Select(x => x.Value.ToUpper()))
        };

        public override void HandleToastActivation(ToastNotificationActivatedEventArgsCompat args)
        {
            if (Supporting.HandleToastActivation(this, args, AppInsights))
            {
                return;
            }
            var arguments = ToastArguments.Parse(args.Argument);
            arguments.TryGetValue("action", out var action);
            arguments.TryGetValue("sender", out var sender);
            LogUse($"ToastBait-{action}-{sender}");
            if (HandleToastActivationInternal(action, sender, arguments))
            {
                return;
            }
            base.HandleToastActivation(args);
        }

        #region Internal Methods

        internal void LogUse(string action, bool forceLog = false, double? count = null, double? duration = null, bool oldAppInsights = true, bool newAppInsights = false)
        {
            // Will be done in the WriteEvent when my PR #1409 is accepted, remove this line then
            LogInfo($"{action}{(count != null ? $" Count: {count}" : "")}{(duration != null ? $" Duration: {duration}" : "")}");
            if (oldAppInsights)
            {
                if (aiOld == null)
                {
                    if (string.IsNullOrEmpty(ToolName))
                    {
                        return;
                    }
                    aiOld = new AppInsights(aiEndpoint, aiKey1, Assembly.GetExecutingAssembly(), ToolName);
                }
                aiOld?.WriteEvent(action, count, duration, HandleAIResult);
            }
            if (newAppInsights)
            {
                if (aiNew == null)
                {
                    if (string.IsNullOrEmpty(ToolName))
                    {
                        return;
                    }
                    aiNew = new AppInsights(aiEndpoint, aiKey2, Assembly.GetExecutingAssembly(), ToolName);
                }
                aiNew?.WriteEvent(action, count, duration, HandleAIResult);
            }
        }

        internal void SaveImageFromBase64(string path, string base64Image)
        {
            try
            {
                if (string.IsNullOrEmpty(base64Image))
                {
                    return;
                }

                var imagePath = path;
                var hashPath = Path.ChangeExtension(imagePath, "hash");

                var folder = Path.GetDirectoryName(imagePath);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                // Compute a simple hash of the base64 content to detect changes
                var currentHash = ComputeHash(base64Image);

                // Check if image needs to be updated
                var needsUpdate = true;
                if (File.Exists(imagePath) && File.Exists(hashPath))
                {
                    var existingHash = File.ReadAllText(hashPath);
                    needsUpdate = !string.Equals(existingHash, currentHash, StringComparison.Ordinal);
                }

                if (needsUpdate)
                {
                    var bytes = Convert.FromBase64String(base64Image);
                    File.WriteAllBytes(imagePath, bytes);
                    File.WriteAllText(hashPath, currentHash);
                }
            }
            catch
            {
                // Silently fail if unable to save image
            }
        }

        internal string ToolAcronym => Acronym(ToolName);

        #endregion Internal Methods

        #region Private Methods

        private string GetRepoName()
        {
            return Name;
        }

        private void HandleAIResult(string result)
        {
            if (!string.IsNullOrEmpty(result))
            {
                LogError("Failed to write to Application Insights:\n{0}", result);
            }
        }

        private static string ComputeHash(string content)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        #endregion Private Methods
    }
}