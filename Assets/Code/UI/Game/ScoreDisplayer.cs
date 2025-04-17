using Code.Services.LocalProgress;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Game
{
    public class ScoreDisplayer : MonoBehaviour
    {
        [SerializeField] private Text _levelText;

        private int _currentScore = 0;
        
        private ILevelLocalProgressService _localProgressService;
        
        [Inject]
        public void Constructor(ILevelLocalProgressService localProgressService)
        {
            _localProgressService = localProgressService;
        }
        
        public void Initialize()
        {
            _currentScore = 0;
            _levelText.text = "";
            
            SetScoreText(0);
            SubscribeEvent();
        }

        public void Dispose()
        {
            UnsubscribeEvent();
        }
        
        private void SubscribeEvent()
        {
            _localProgressService.UpdateScoreEvent += SetScoreText;
        }
        
        private void UnsubscribeEvent()
        {
            _localProgressService.UpdateScoreEvent -= SetScoreText;
        }
        
        private void SetScoreText(int score)
        {
            DOTween.To(() => _currentScore, x =>
            {
                _currentScore = x;
                _levelText.text = "Score: " + _currentScore;
            }, score, 1f).SetEase(Ease.OutQuad);
        }
    }
}