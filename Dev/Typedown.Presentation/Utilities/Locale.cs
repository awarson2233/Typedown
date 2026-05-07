using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Typedown.Core.Utilities;

namespace Typedown.Presentation.Utilities
{
    public static class Locale
    {
        public enum ResourceSource
        {
            All,
            CommonResources,
            DialogResources,
            SettingsResources,
            Resources
        }

        public static Func<string, ResourceSource, string> StringResolver { get; set; } = (key, _) => key;

        public static Dictionary<string, string> SupportedLangs { get; } = new()
        {
            {"af","Afrikaans"},
            {"am","አማርኛ"},
            {"ar","العربية"},
            {"as","অসমীয়া"},
            {"bg","Български"},
            {"bn","বাংলা"},
            {"bs","Bosnian"},
            {"ca","Català"},
            {"cs","Čeština"},
            {"cy","Cymraeg"},
            {"da","Dansk"},
            {"de","Deutsch"},
            {"el","Ελληνικά"},
            {"en","English"},
            {"es","Español"},
            {"et","Eesti"},
            {"eu","Euskara"},
            {"fa","فارسی"},
            {"fi","Suomi"},
            {"fil","Filipino"},
            {"fr","Français"},
            {"ga","Gaeilge"},
            {"gl","Galego"},
            {"gu","ગુજરાતી"},
            {"he","עברית"},
            {"hi","हिन्दी"},
            {"hr","Hrvatski"},
            {"hu","Magyar"},
            {"hy","Հայերեն"},
            {"id","Indonesia"},
            {"is","Íslenska"},
            {"it","Italiano"},
            {"ja","日本語"},
            {"ka","ქართული"},
            {"kk","Қазақ Тілі"},
            {"km","ខ្មែរ"},
            {"kn","ಕನ್ನಡ"},
            {"ko","한국어"},
            {"lo","ລາວ"},
            {"lt","Lietuvių"},
            {"lv","Latviešu"},
            {"mi","Te Reo Māori"},
            {"mk","Македонски"},
            {"ml","മലയാളം"},
            {"mr","मराठी"},
            {"ms","Melayu"},
            {"mt","Malti"},
            {"nb","Norsk Bokmål"},
            {"ne","नेपाली"},
            {"nl","Nederlands"},
            {"or","ଓଡ଼ିଆ"},
            {"pa","ਪੰਜਾਬੀ"},
            {"pl","Polski"},
            {"prs","دری"},
            {"pt","Português (Brasil)"},
            {"ro","Română"},
            {"ru","Русский"},
            {"sk","Slovenčina"},
            {"sl","Slovenščina"},
            {"sq","Shqip"},
            {"sr-Latn","Srpski (latinica)"},
            {"sv","Svenska"},
            {"sw","Kiswahili"},
            {"ta","தமிழ்"},
            {"te","తెలుగు"},
            {"th","ไทย"},
            {"ti","ትግር"},
            {"tr","Türkçe"},
            {"uk","Українська"},
            {"ur","اردو"},
            {"uz","Uzbek (Latin)"},
            {"vi","Tiếng Việt"},
            {"zh-Hans","中文 (简体)"},
            {"zh-Hant","繁體中文 (繁體)"},
            {"zu","Isi-Zulu"},
        };

        public static IReadOnlyDictionary<string, string> LangsOptions =>
            new Dictionary<string, string>(SupportedLangs.Append(new("default", GetString("UseSystemSetting"))));

        public static string GetLangOptionDisplayName(string key) => LangsOptions[key];

        public static string GetString(string key, ResourceSource source = 0)
        {
            return StringResolver(key, source);
        }

        public static string GetDialogString(string key)
        {
            return GetString(key, ResourceSource.DialogResources);
        }

        public static string? GetTypeString(Type type)
        {
            return (type.GetCustomAttribute(typeof(LocaleAttribute)) as LocaleAttribute)?.Text;
        }
    }
}
