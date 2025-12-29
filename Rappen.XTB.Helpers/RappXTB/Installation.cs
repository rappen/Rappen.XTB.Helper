using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XrmToolBox.AppCode.AppInsights;
using XrmToolBox.Extensibility;

namespace Rappen.XTB.Helpers.RappXTB
{
    public class Installation
    {
        private const string FileName = "Rappen.XTB.xml";
        private int settingsversion = -1;

        // Semaphore to prevent concurrent writes from multiple threads
        private static readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        public int SettingsVersion
        {
            get => settingsversion;
            set
            {
                if (settingsversion != -1 && value != settingsversion && Tools?.Count() > 0)
                {
                    Tools.ForEach(s => s.Support.AutoDisplayCount = 0);
                }
                settingsversion = value;
            }
        }

        public Guid Id = Guid.Empty;
        public DateTime FirstRunDate = DateTime.Now;
        public string CompanyName;
        public string CompanyEmail;
        public string CompanyCountry;
        public bool SendInvoice;
        public string PersonalFirstName;
        public string PersonalLastName;
        public string PersonalEmail;
        public string PersonalCountry;
        public bool PersonalContactMe;
        public List<Tool> Tools = new List<Tool>();

        public static Installation Load()
        {
            var result = XmlAtomicStore.Deserialize<Installation>(Path.Combine(Paths.SettingsPath, FileName));
            result.Normalize();
            result.Tools.ForEach(t => t.Installation = result);
            return result;
        }

        internal void Normalize()
        {
            if (Id.Equals(Guid.Empty))
            {
                Id = InstallationInfo.Instance.InstallationId;
            }
            if (Settings.Instance.SettingsVersion != settingsversion)
            {
                SettingsVersion = Settings.Instance.SettingsVersion;
            }
        }

        /// <summary>
        /// Asynchronously saves the installation data.
        /// Uses a semaphore to guard against concurrent writes
        /// and an atomic temp-file replace to prevent corruption.
        /// </summary>
        public Task SaveAsync(CancellationToken cancellationToken = default, bool runAsync = true)
        {
            var path = Path.Combine(Paths.SettingsPath, FileName);
            return XmlAtomicStore.SerializeAsync(this, path, runAsync: runAsync, writeThrough: true, useNamedMutex: true, cancellationToken: cancellationToken);
        }

        public void Remove()
        {
            var path = Path.Combine(Paths.SettingsPath, FileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public Tool this[string name]
        {
            get
            {
                if (!Tools.Any(t => t.Name == name))
                {
                    Tools.Add(new Tool(this, name));
                }
                return Tools.FirstOrDefault(t => t.Name == name);
            }
        }

        public override string ToString() => $"{CompanyName} {PersonalFirstName} {PersonalLastName} {Tools.Count}".Trim().Replace("  ", " ");
    }

    public class Tool
    {
        private Version _version;

        internal string Acronym => RappPluginControlBase.Acronym(Name);
        internal Installation Installation;

        internal Version version
        {
            get => _version;
            set
            {
                if (value != _version)
                {
                    Support.AutoDisplayCount = 0;
                    if (Support?.Type == SupportType.Never)
                    {
                        Support.Type = SupportType.None;
                    }
                }
                _version = value;
            }
        }

        public string Name { get; set; }

        public string Version
        {
            get => _version.ToString();
            set { _version = new Version(value ?? "0.0.0.0"); }
        }

        public int ExecuteCount;

        public DateTime FirstRunDate = DateTime.Now;
        public DateTime VersionRunDate;
        public Support Support = new Support();

        private Tool()
        { }

        internal Tool(Installation installation, string name)
        {
            Installation = installation;
            Name = name;
        }

        public string GetUrlCorp(bool validate = true)
        {
            if (validate)
            {
                if (string.IsNullOrEmpty(Installation.CompanyName) ||
                    string.IsNullOrEmpty(Installation.CompanyEmail) ||
                    string.IsNullOrEmpty(Installation.CompanyCountry))
                {
                    return null;
                }
            }
            return GenerateUrl(Settings.Instance.FormUrlCorporate, Settings.Instance.FormIdCorporate);
        }

        public string GetUrlPersonal(bool contribute, bool validate = true)
        {
            if (validate)
            {
                if (string.IsNullOrEmpty(Installation.PersonalFirstName) ||
                    string.IsNullOrEmpty(Installation.PersonalLastName) ||
                    string.IsNullOrEmpty(Installation.PersonalEmail) ||
                    string.IsNullOrEmpty(Installation.PersonalCountry))
                {
                    return null;
                }
            }
            return GenerateUrl(contribute ? Settings.Instance.FormUrlContribute : Settings.Instance.FormUrlSupporting, contribute ? Settings.Instance.FormIdContribute : Settings.Instance.FormIdPersonal);
        }

        public string GetUrlAlready()
        {
            return GenerateUrl(Settings.Instance.FormUrlAlready, Settings.Instance.FormIdAlready);
        }

        public string GetUrlGeneral()
        {
            return GenerateUrl(Settings.Instance.FormUrlGeneral, Settings.Instance.FormIdCorporate);
        }

        private string GenerateUrl(string template, string form)
        {
            return template
                .Replace("{formid}", form)
                .Replace("{company}", Installation.CompanyName)
                .Replace("{invoiceemail}", Installation.CompanyEmail)
                .Replace("{companycountry}", Installation.CompanyCountry)
                .Replace("{amount}", Support.Amount)
                .Replace("{size}", Support.UsersCount)
                .Replace("{sendinvoice}", Installation.SendInvoice ? "Send%20me%20an%20invoice" : "")
                .Replace("{firstname}", Installation.PersonalFirstName)
                .Replace("{lastname}", Installation.PersonalLastName)
                .Replace("{email}", Installation.PersonalEmail)
                .Replace("{country}", Installation.PersonalCountry)
                .Replace("{contactme}", Installation.PersonalContactMe ? "Contact%20me%20after%20submitting%20this%20form!" : "")
                .Replace("{tool}", Name)
                .Replace("{version}", version.ToString())
                .Replace("{instid}", Installation.Id.ToString());
        }

        public override string ToString() => $"{Name} {version}";
    }

    public class Support
    {
        public DateTime AutoDisplayDate = DateTime.MinValue;
        public DateTime ToastedDate = DateTime.MinValue;
        public int AutoDisplayCount;
        public DateTime SubmittedDate;
        public SupportType Type = SupportType.None;
        public int UsersIndex;
        public string UsersCount;

        public string Amount
        {
            get
            {
                switch (UsersIndex)
                {
                    case 1:
                        return "X-Small";

                    case 2:
                        return "Small";

                    case 4:
                        return "Large";

                    case 5:
                        return "X-Large";

                    default:
                        return "Medium";
                }
            }
        }

        public override string ToString() => $"{Type}";
    }

    public enum SupportType
    {
        None,
        Personal,
        Company,
        Contribute,
        Already,
        Never
    }
}