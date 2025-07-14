using UnityEngine;

namespace Code.Localization.Code
{
    public abstract class LocalizeBase : MonoBehaviour
    {
        public string localizationKey;

        public abstract void UpdateLocale();

        public static string GetLocalizedString(string key)
        {
            if (Locale.currentLanguageStrings.ContainsKey(key))
                return Locale.currentLanguageStrings[key];
            
            return string.Empty;
        }
    }
}