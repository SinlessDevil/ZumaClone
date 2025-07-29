using TMPro;
using UnityEngine;

namespace Code.Localization.Code
{
    [DisallowMultipleComponent]
    public class TMP_Localizer : LocalizeBase
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private bool _isDynamic = false;

        private void OnValidate()
        {
            if(_text == null) 
                _text = GetComponent<TMP_Text>();
        }
        
        private void Start()
        {
            if(!_isDynamic) 
                UpdateLocale();
        }
        
        public override void UpdateLocale()
        {
            if (!_text)
            {
                Debug.LogError($"TMP_Localizer: Missing TMP_Text component on {gameObject.name}");
                return;
            }

            if (!System.String.IsNullOrEmpty(localizationKey) && 
                Locale.currentLanguageStrings.ContainsKey(localizationKey))
            {
                _text.text = Locale.currentLanguageStrings[localizationKey].Replace(@"\n", "" + '\n');   
            }
        }
    }
}