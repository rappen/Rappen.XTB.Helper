using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.AI.WinForm
{
    public class AiSettings
    {
        public string Provider { get; set; }
        public string Model { get; set; }
        public string Endpoint { get; set; }    // When the user has her own provider/model
        public string ApiKey { get; set; }
        public string MyName { get; set; }
        public int Calls { get; set; }
        public bool LogConversation { get; set; }
        public bool PreferDisplayName { get; set; }
        public bool SendWithEnter { get; set; }

        public override string ToString() => $"{Provider} - {Model} - {Endpoint} - {ApiKey} - {MyName} - {PreferDisplayName}";
    }

    public class AiSupport
    {
        public string TextToRequestFreeAi { get; set; }
        public string OnlyInfoName { get; set; }
        public Prompts Prompts { get; set; } = new Prompts();
        public List<AiProvider> AiProviders { get; set; } = new List<AiProvider>();
        public List<PopupByCallNo> PopupByCallNos { get; set; } = new List<PopupByCallNo>();
        public string UrlToUseForFree { get; set; } = "https://jonasr.app/fxb/free-ai-chat/";
        public string WpfToUseForFree { get; set; } = "18554";
        public string AppRegistrationEndpoint { get; set; } = "https://dc.services.visualstudio.com/v2/track";
        public Guid InstrumentationKey { get; set; } = new Guid("b9674a37-ff73-4187-8504-482a9e9403fb");

        public AiSupport() { }

        public List<AiProvider> SupportedAiProviders(Version version) => AiProviders
            .Where(n => string.IsNullOrEmpty(n.FromVersion) || Version.TryParse(n.FromVersion, out var fromVersion) && fromVersion <= version)
            .Where(n => string.IsNullOrEmpty(n.ToVersion) || Version.TryParse(n.ToVersion, out var toVersion) && toVersion >= version)
            .ToList();

        public AiProvider Provider(string aiprovider) => AiProviders.FirstOrDefault(n => n.ToString().Equals(aiprovider));
    }

    public class Prompts
    {
        public string System { get; set; }
        public string Format { get; set; }
        public string CallMe { get; set; }
        public string PreferNames { get; set; }
        public string Update { get; set; }
        public string EntityMeta { get; set; }
        public string AttributeMeta { get; set; }
    }

    public class AiProvider
    {
        private const int interval = 5;
        public string Name { get; set; }
        public string FullName { get; set; }
        public string FromVersion { get; set; }
        public string ToVersion { get; set; }
        public string Url { get; set; }
        public bool EndpointFixed { get; set; }
        public string ApiKey { get; set; }
        public bool Free { get; set; }
        public Prompts Prompts { get; set; }
        public List<AiModel> Models { get; set; } = new List<AiModel>();

        public AiModel Model(string model) => Models?.FirstOrDefault(n => n.Name.Equals(model));

        public override string ToString() => string.IsNullOrWhiteSpace(FullName) ? Name : FullName;

        internal string ApiKeyDecrypted
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ApiKey))
                {
                    return string.Empty;
                }
                var x = "";
                for (var i = 0; i < ApiKey.Length; i++)
                {
                    if ((i + 1) % (interval + 1) != 0)
                    {
                        x += ApiKey[i];
                    }
                }

                try
                {
                    return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(x));
                }
                catch
                {
                    return string.Empty;
                }
            }
            set
            {
                var random = new Random();
                ApiKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
                for (var i = interval; i < ApiKey.Length; i += interval + 1)
                {
                    ApiKey = ApiKey.Insert(i, ((char)random.Next('a', 'z')).ToString());
                }
            }
        }
    }

    public class AiModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Endpoint { get; set; }    // For this who are fixed for this provider/model
        public bool? LogConversation { get; set; } = null;
        public Prompts Prompts { get; set; }

        public override string ToString() => Name;
    }

    public class PopupByCallNo
    {
        public int StartAtCallNo { get; set; }
        public int RepeatEvery { get; set; } = 0; // 0 means no repeat
        public int StopAtCallNo { get; set; } = 0; // 0 means no stop
        public bool SuggestsSupporting { get; set; } = true; // Only show this popup if the user has not supported the tool yet
        public bool ForFreeProviders { get; set; } = false; // Only show this popup if the user is using a free provider
        public string Title { get; set; }
        public string Message { get; set; }
        public string HelpUrl { get; set; }

        public bool TimeToPopup(int CallNo, bool IsSupporting, bool IsFree)
        {
            if (CallNo < StartAtCallNo)
            {   // Too early, no popup
                return false;
            }
            if (StopAtCallNo > 0 && CallNo > StopAtCallNo)
            {   // Too many popups, exit now
                return false;
            }
            if (SuggestsSupporting && IsSupporting)
            {   // User has already supported the tool, no need to suggest again
                return false;
            }
            if (ForFreeProviders && !IsFree)
            {   // User is not using a free provider, no need to popup
                return false;
            }
            if (CallNo == StartAtCallNo)
            {   // First call, always show popup
                return true;
            }
            if (RepeatEvery == 0)
            {   // No repeat, only show once
                return false;
            }
            if ((CallNo - StartAtCallNo) % RepeatEvery == 0)
            {   // Repeat every N calls
                return true;
            }
            return false;
        }
    }
}