using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Code.Localization.Code.Services.LocalizeLanguageService
{
    public class LocalizeLanguageService : ILocalizeLanguageService
    {
        private const string ResourcesPath = "Localization";

        public void InitLanguage()
        {
            SetLanguage(GetCurrentLanguage().ToString());
        }

        public List<string> GetAvailableLanguages()
        {
            TextAsset[] files = Resources.LoadAll<TextAsset>(ResourcesPath);
            return files.Select(file => Path.GetFileNameWithoutExtension(file.name)).ToList();
        }

        public void SetLanguage(string languageName)
        {
            Locale.CurrentLanguage = languageName;
            Locale.PlayerLanguage = LanguageNameToSystemLanguage(languageName);
            
            LocalizeBase[] allTexts = Object.FindObjectsOfType<LocalizeBase>();
            foreach (var t in allTexts)
                t.UpdateLocale();
        }

        public SystemLanguage GetCurrentLanguage() => Locale.PlayerLanguage;

        private SystemLanguage LanguageNameToSystemLanguage(string name)
        {
            if (System.Enum.TryParse<SystemLanguage>(name, true, out var lang))
                return lang;
            
            Debug.LogWarning($"⚠Unknown language '{name}', fallback to English.");
            
            return SystemLanguage.English;
        }
    }
}