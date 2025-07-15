using Code.Services.Levels;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.UI.Game
{
    public class LevelDisplayer : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;

        private ILevelService _levelService;
        
        [Inject]
        public void Constructor(ILevelService levelService)
        {
            _levelService = levelService;
        }
        
        public void Initialize()
        {
            _levelText.text = "";
            
            string nameLevel = _levelService.GetCurrentLevelStaticData().LevelName;
            string numberLevel = _levelService.GetCurrentChapter() + "-" + _levelService.GetCurrentLevel();
            string level = nameLevel + " " + numberLevel;
            
            SetLevelText(level);
        }
        
        private void SetLevelText(string text)
        {
            _levelText.text = text;
        }
    }   
}