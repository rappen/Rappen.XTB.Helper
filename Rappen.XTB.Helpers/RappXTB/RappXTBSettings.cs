using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using XrmToolBox.Extensibility;

namespace Rappen.XTB.Helpers.RappXTB
{
    public class RappXTBSettings
    {
        internal static readonly string URL = "https://rappen.github.io/Tools/";
        private const string SettingsFileName = "Rappen.XTB.Settings.xml";
        private static RappXTBSettings instance;

        public int SettingsVersion = 1;
        public int ToastImageCacheHours = 24;
        public List<SupportableTool> SupportableTools = new List<SupportableTool>();
        public List<ToastableTool> ToastableTools = new List<ToastableTool>();
        public int ShowMinutesAfterToolInstall = int.MaxValue;    // 60
        public int ShowMinutesAfterToolNewVersion = int.MaxValue; // 120
        public int ShowMinutesAfterSupportingShown = int.MaxValue; // 2880m / 48h / 2d
        public int ShowMinutesAfterSubmitting = int.MaxValue; // 2880m / 48h / 2d
        public int ShowAutoPercentChance = 0;   // Moved to each tool
        public int ShowAutoRepeatTimes = -1; // 10
        public int ResetUnfinalizedSupportingAfterDays = int.MaxValue; // 7
        public bool BMACLinkPositionRandom = false;
        public bool CloseLinkPositionRandom = false;
        public int CloseLinkHorizFromOrigMin = -90;
        public int CloseLinkHorizFromOrigMax = 0;
        public int CloseLinkVertiFromOrigMin = -50;
        public int CloseLinkVertiFromOrigMax = 0;

        public string FormIdCorporate = "wpf17273";
        public string FormIdPersonal = "wpf17612";
        public string FormIdContribute = "wpf17677";
        public string FormIdAlready = "wpf17761";

        public string FormUrlCorporate =
            "https://jonasr.app/supporting-prefilled/" +
            "?{formid}_1_first={firstname}" +
            "&{formid}_1_last={lastname}" +
            "&{formid}_3={companycountry}" +
            "&{formid}_4={invoiceemail}" +
            "&{formid}_13={tool}" +
            //"&{formid}_19={size}" +
            //"&{formid}_24={amount}" +
            "&{formid}_27={company}" +
            "&{formid}_37={sendinvoice}" +
            "&{formid}_31={tool}" +
            "&{formid}_32={version}" +
            "&{formid}_33={instid}";

        public string FormUrlSupporting =
            "https://jonasr.app/supporting/personal-prefilled/" +
            "?{formid}_1_first={firstname}" +
            "&{formid}_1_last={lastname}" +
            "&{formid}_3={country}" +
            "&{formid}_4={email}" +
            "&{formid}_52={contactme}" +
            "&{formid}_13={tool}" +
            "&{formid}_31={tool}" +
            "&{formid}_32={version}" +
            "&{formid}_33={instid}";

        public string FormUrlContribute =
            "https://jonasr.app/supporting/contribute-prefilled/" +
            "?{formid}_1_first={firstname}" +
            "&{formid}_1_last={lastname}" +
            "&{formid}_3={country}" +
            "&{formid}_4={email}" +
            "&{formid}_52={contactme}" +
            "&{formid}_13={tool}" +
            "&{formid}_31={tool}" +
            "&{formid}_32={version}" +
            "&{formid}_33={instid}";

        public string FormUrlAlready =
            "https://jonasr.app/supporting/already/" +
            "?{formid}_1_first={firstname}" +
            "&{formid}_1_last={lastname}" +
            "&{formid}_3={country}" +
            "&{formid}_4={email}" +
            "&{formid}_13={tool}" +
            "&{formid}_31={tool}" +
            "&{formid}_32={version}" +
            "&{formid}_33={instid}";

        public string FormUrlGeneral =
            "https://jonasr.app/supporting/" +
            "?{formid}_13={tool}" +
            "&{formid}_31={tool}" +
            "&{formid}_32={version}" +
            "&{formid}_33={instid}";

        public string ColorBg = "FF0042AD";                     // FF0042AD Dark blue
        public string ColorTextFgNormal = "FFFFFF00";           // FFFFFF00 Yellow
        public string ColorTextFgDimmed = "FFD2B48C";           // FFD2B48C Dim yellow
        public string ColorFieldBgNormal = "FF0063FF";          // FF0063FF Light blue
        public string ColorFieldBgInvalid = "FFF06565";         // FFF06565 Dim red

        public Color clrBackground => GetColor(ColorBg, "FF0042AD");
        public Color clrTxtFgNormal => GetColor(ColorTextFgNormal, "FFFFFF00");
        public Color clrTxtFgDimmed => GetColor(ColorTextFgDimmed, "FFD2B48C");
        public Color clrFldBgNormal => GetColor(ColorFieldBgNormal, "FF0063FF");
        public Color clrFldBgInvalid => GetColor(ColorFieldBgInvalid, "FFF06565");

        public string ConfirmDirecting = @"You will now be redirected to the website form
to finish Your flavor of support.
After the form is submitted, Jonas will handle it soon.

NOTE: It has to be submitted during the next step!";

        public string ToastHeader = "Support {tool} to keep it evolving!";
        public string ToastText = "⏱️ Saving time.\n💰 Making money.\n☕ More coffee.";
        public string ToastAttrText = "Be Rapp-id — pick one below, because legendary tools don’t build themselves!";
        public string ToastButtonCorporate = "Corporate Support";
        public string ToastButtonPersonal = "Personal Support";

        public string StatusDefaultText = "Click here if\r\nYou are already\r\nsupporting!";
        public string StatusDefaultTip = "If you have already supported\r\n{tool}\r\nin any way - Click here to let me know,\r\nand this popup will not appear again!";
        public string StatusCompanyText = "Your company\r\nare supporting\r\n{tool}!";
        public string StatusCompanyTip = "We know that your company is supporting\r\n{tool}\r\nThank You!";
        public string StatusPersonalText = "You are\r\nsupporting\r\n{tool}!";
        public string StatusPersonalTip = "We know that you are supporting\r\n{tool}\r\nThank You!";
        public string StatusContributeText = "You are\r\ncontributing to\r\n{tool}!";
        public string StatusContributeTip = "We know that you are contributing to\r\n{tool}\r\nThank You!";
        public string StatusAlreadyText = "You have already\r\nsupported\r\n{tool}!";
        public string StatusAlreadyTip = "We know that you have already supported\r\n{tool}\r\nThank You!";
        public string StatusNeverText = "You will never\r\nsupport\r\n{tool}.";
        public string StatusNeverTip = "For some strange reason,\r\nyou will never support\r\n{tool}\r\nThink again? 😉";
        public string StatusPendingText = "You have recently\r\nsupported.\r\nJonas is processing it\r\n(if You finalized it).";
        public string StatusPendingTip = "It may take hours/days to process the support...\r\nJonas will handle it after you have finalized the web form.\r\n\r\nThank You so much! ❤️";

        public string HelpWhyTitle = "Community Tools are Conscienceware.";

        public string HelpWhyText = @"Why Support the Tools You Use?

In the Power Platform community, some people create tools, others share ideas, write documentation, and fix bugs. Most simply use these tools—and that’s perfectly fine!

But think about it: when you watch TV or stream music, you pay for Netflix, Amazon Prime, or Spotify. Why? Because you value what you get.

Open-source tools are free to use, but they’re not free to build or maintain. If you benefit from them—saving time, improving quality—consider giving back. Support isn’t just about money; it’s about acknowledging the value you’ve received.

Especially if you work in a large corporation, using free tools to drive revenue, supporting the community is the right thing to do. It’s about fairness and sustainability.

You’re already part of the community by using these tools. Now you can support them formally—not just with a coffee donation, but in a way that helps development and maintenance.

Want to learn more? Read my thoughts here: https://jonasr.app/helping/

Let’s make “Conscienceware” a reality.

- Jonas Rapp";

        public string HelpInfoTitle = "Technical Information";

        public string HelpInfoText = @"Your entered name, company, country, email, and amount will not be stored in any official system. The information will only be saved in my personal Excel file and in my own Power Platform app, mostly for me, myself and I to learn even more about how the platform could help us. I also do this to ensure that you can get an invoice, and if needed we need to communicate by email.
The email you share with me, only to me, will never be sold to any company. I won't try to sell anything. Period.

You will receive an official receipt immediately and, if needed, an invoice. Supporting can be done with a credit card. Other options like Google Pay will be available depending on your location. Stripe handles the payment.

When you click the big button here, the information you entered here will be included in the form on my website, jonasr.app, and a few hidden info: tool name, version, and your XrmToolBox 'InstallationId' (a random Guid generated the first time you use the toolbox). If you are curious, you can see how to find your ID on this link: https://jonasr.app/xtb-finding-installationid.

Since I would like to be very clear and transparent - we store your XrmToolBox InstallationId on a server to be able to know that this installation is supporting it in some way. There is nothing about your name, the amount or contribution; I am not interested in hacking this info.

The button in the top-right corner opens this info. You can also right-click on it and find more options, especially:
* I have already supported this tool — use this to tell me that you already support this tool in some way so that this popup prompt will not ask you again.
* I will never support this tool — use it if you think it is a bad idea, and you probably won't use the tool again; it won't ask you again.

For questions, contact me at https://jonasr.app/contact.";

        public RappXTBSettings()
        { }

        public static RappXTBSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Get();
                }
                return instance;
            }
        }

        public static void Reset() => instance = null;

        private static RappXTBSettings Get() => XmlAtomicStore.DownloadXml<RappXTBSettings>(URL, SettingsFileName, Paths.SettingsPath);

        public SupportableTool this[string name]
        {
            get
            {
                if (!SupportableTools.Any(st => st.Name == name))
                {
                    SupportableTools.Add(new SupportableTool { Name = name });
                }
                return SupportableTools.FirstOrDefault(st => st.Name == name);
            }
        }

        public ToastableTool GetToastableTool(string name)
        {
            if (!ToastableTools.Any(tt => tt.Name == name))
            {
                ToastableTools.Add(new ToastableTool { Name = name });
            }
            return ToastableTools.FirstOrDefault(tt => tt.Name == name);
        }

        public void Save() => XmlAtomicStore.SerializeAsync(this, Path.Combine(Paths.SettingsPath, SettingsFileName));

        private Color GetColor(string color, string defaultColor)
        {
            int intColor;
            try
            {
                intColor = int.Parse(color, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                intColor = int.Parse(defaultColor, System.Globalization.NumberStyles.HexNumber);
            }
            return Color.FromArgb(intColor);
        }
    }

    public class SupportableTool
    {
        public string Name;
        public bool Enabled = false;
        public bool ShowAutomatically = false;
        public bool ContributionCounts = true;
        public int ShowAutoPercentChance = 0;   // 25 (0-100)
    }

    public class ToastableTool
    {
        public string Name;
        public bool Enabled = false;
        public int ExecuteStart = 0;            // 40 - minimum executions before toast
        public int ExecuteEnd = 0;              // 1000 - maximum executions to toast
        public int ExecuteInterval = 0;         // 70 - every 70'th execution
        public int ExecutePercentChance = 0;    // 5 % - random chance to toast when execute
        public int OpenPercentChance = 0;       // 20 % - random chance to toast when opening the tool
        public int MinutesBetweenToasts = 0;    // 60 - minimum minutes between toasts
    }
}