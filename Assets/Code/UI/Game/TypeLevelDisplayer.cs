using Code.Services.Levels;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.UI.Game
{
    public class TypeLevelDisplayer : MonoBehaviour
    {
        [SerializeField] private TMP_Text _typeLevelText;

        private ILevelService _levelService;
        
        [Inject]
        public void Constructor(ILevelService levelService)
        {
            _levelService = levelService;
        }
        
        public void Initialize()
        {
            _typeLevelText.text = "";
            
            string levelTypeId = _levelService.GetCurrentLevelStaticData().LevelTypeId.ToString();
            SetLevelText(levelTypeId);
        }
        
        private void SetLevelText(string text)
        {
            _typeLevelText.text = text;
        }

    }   
}