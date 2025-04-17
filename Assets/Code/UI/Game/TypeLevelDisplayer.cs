using Code.Services.Levels;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Game
{
    public class TypeLevelDisplayer : MonoBehaviour
    {
        [SerializeField] private Text _typeLevelText;

        private ILevelService _levelService;
        
        [Inject]
        public void Constructor(ILevelService levelService)
        {
            _levelService = levelService;
        }
        
        public void Initialize()
        {
            _typeLevelText.text = "";
            
            var levelTypeId = _levelService.GetCurrentLevelStaticData().LevelTypeId.ToString();
            SetLevelText(levelTypeId);
        }
        
        private void SetLevelText(string text)
        {
            _typeLevelText.text = text;
        }

    }   
}