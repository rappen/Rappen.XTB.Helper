using System;
using System.Collections.Generic;
using System.Linq;

namespace Rappen.AI.WinForm
{
    public class AiSettings
    {
        public string Supplier { get; set; }
        public string Model { get; set; }
        public string ApiKey { get; set; }
        public string MyName { get; set; }
        public int Calls { get; set; }
        public bool LogConversation { get; set; } = true;
    }

    public class AiSupport
    {
        public Prompts Prompts { get; set; } = new Prompts();
        public List<AiSupplier> AiSuppliers { get; set; } = new List<AiSupplier>();
        public List<PopupByCallNo> PopupByCallNos { get; set; } = new List<PopupByCallNo>();
        public string UrlToUseForFree { get; set; } = "https://jonasr.app/fxb/free-ai-chat/";
        public string WpfToUseForFree { get; set; } = "18554";
        public string AppRegistrationEndpoint { get; set; } = "https://dc.services.visualstudio.com/v2/track";
        public Guid InstrumentationKey { get; set; } = new Guid("b9674a37-ff73-4187-8504-482a9e9403fb");

        public AiSupport() { }

        public AiSupplier Supplier(string aisupplier) => AiSuppliers.FirstOrDefault(n => n.Name.Equals(aisupplier));
    }

    public class Prompts
    {
        public string System { get; set; }
        public string CallMe { get; set; }
        public string Update { get; set; }
        public string EntityMeta { get; set; }
        public string AttributeMeta { get; set; }
    }

    public class AiSupplier
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public Prompts Prompts { get; set; } = new Prompts();
        public List<AiModel> Models { get; set; } = new List<AiModel>();

        public AiModel Model(string model) => Models?.FirstOrDefault(n => n.Name.Equals(model));

        public override string ToString() => Name;

        public bool IsFree => Name.ToLowerInvariant().Contains("free");
    }

    public class AiModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public Prompts Prompts { get; set; } = new Prompts();

        public override string ToString() => Name;
    }

    public class PopupByCallNo
    {
        public int StartAtCallNo { get; set; }
        public int RepeatEvery { get; set; } = 0; // 0 means no repeat
        public int StopAtCallNo { get; set; } = 0; // 0 means no stop
        public bool SuggestsSupporting { get; set; } = true; // Only show this popup if the user has not supported the tool yet
        public string Message { get; set; }
        public string HelpUrl { get; set; }

        public bool TimeToPopup(int CallNo, bool IsSupporting)
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