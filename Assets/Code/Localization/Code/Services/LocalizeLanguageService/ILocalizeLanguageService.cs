using System.Collections.Generic;
using UnityEngine;

namespace Code.Localization.Code.Services.LocalizeLanguageService
{
    public interface ILocalizeLanguageService
    {
        public void InitLanguage();
        public List<string> GetAvailableLanguages();
        public void SetLanguage(string languageName);
        public SystemLanguage GetCurrentLanguage();
    }   
}