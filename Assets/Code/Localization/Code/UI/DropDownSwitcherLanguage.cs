using System.Collections.Generic;
using Code.Localization.Code.Services.LocalizeLanguageService;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.Localization.Code.UI
{
    public class DropDownLanguageSwitcher : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _dropdown;
        private ILocalizeLanguageService _localizeLanguageService;

        private bool _initialized;

        private void OnValidate()
        {
            if (_dropdown == null)
                _dropdown = GetComponent<TMP_Dropdown>();
        }
        
        [Inject]
        public void Construct(ILocalizeLanguageService localizeLanguageService)
        {
            _localizeLanguageService = localizeLanguageService;
        }

        private void Start()
        {
            SetupDropdown();
        }

        private void SetupDropdown()
        {
            _dropdown.ClearOptions();

            List<string> languages = _localizeLanguageService.GetAvailableLanguages();
            _dropdown.AddOptions(languages);
            
            SystemLanguage currentLanguage = _localizeLanguageService.GetCurrentLanguage();
            int currentIndex = languages.IndexOf(currentLanguage.ToString());
            _dropdown.SetValueWithoutNotify(currentIndex >= 0 ? currentIndex : 0);

            _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void OnDropdownValueChanged(int index)
        {
            var selectedLanguage = _dropdown.options[index].text;
            _localizeLanguageService.SetLanguage(selectedLanguage);
        }
    }
}