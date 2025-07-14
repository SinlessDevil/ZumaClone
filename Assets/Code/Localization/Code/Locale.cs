using System.Collections.Generic;
using UnityEngine;

namespace Code.Localization.Code
{
    public static class Locale
    {
        private const string STR_LOCALIZATION_KEY = "locale";
        private const string STR_LOCALIZATION_PREFIX = "localization/";

        private static string _currentLanguage;
        private static TextAsset _currentLocalizationText;
        
        public static bool currentLanguageHasBeenSet = false;
        public static Dictionary<string, string> currentLanguageStrings = new();
        
        public static string CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    _currentLanguage = value;
                    _currentLocalizationText =
                        Resources.Load(STR_LOCALIZATION_PREFIX + _currentLanguage, typeof(TextAsset)) as TextAsset;
                    if (_currentLocalizationText == null)
                    {
                        Debug.LogWarningFormat("Missing locale '{0}', loading English.", _currentLanguage);
                        _currentLanguage = SystemLanguage.English.ToString();
                        _currentLocalizationText =
                            Resources.Load(STR_LOCALIZATION_PREFIX + _currentLanguage, typeof(TextAsset)) as TextAsset;
                    }

                    if (_currentLocalizationText != null)
                    {
                        string[] lines = _currentLocalizationText.text.Split(new string[] { "\r\n", "\n\r", "\n" },
                            System.StringSplitOptions.RemoveEmptyEntries);
                        currentLanguageStrings.Clear();
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string[] pairs = lines[i].Split(new char[] { '\t', '=' }, 2);
                            if (pairs.Length == 2)
                            {
                                currentLanguageStrings.Add(pairs[0].Trim(), pairs[1].Trim());
                            }
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("Local language '{0}', not found!", _currentLanguage);
                    }
                }
            }
        }

        public static bool CurrentLanguageHasBeenSet
        {
            get { return currentLanguageHasBeenSet; }
        }

        public static SystemLanguage PlayerLanguage
        {
            get { return (SystemLanguage)PlayerPrefs.GetInt(STR_LOCALIZATION_KEY, (int)Application.systemLanguage); }
            set
            {
                PlayerPrefs.SetInt(STR_LOCALIZATION_KEY, (int)value);
                PlayerPrefs.Save();
            }
        }
    }
}