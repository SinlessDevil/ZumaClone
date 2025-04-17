using Code.Services.Levels;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Game
{
    public class LevelDisplayer : MonoBehaviour
    {
        [SerializeField] private Text _levelText;

        private ILevelService _levelService;
        
        [Inject]
        public void Constructor(ILevelService levelService)
        {
            _levelService = levelService;
        }
        
        public void Initialize()
        {
            _levelText.text = "";
            
            var nameLevel = _levelService.GetCurrentLevelStaticData().LevelName;
            var numberLevel = _levelService.GetCurrentChapter() + "-" + _levelService.GetCurrentLevel();
            var level = nameLevel + " " + numberLevel;
            SetLevelText(level);
        }
        
        private void SetLevelText(string text)
        {
            _levelText.text = text;
        }
    }   
}