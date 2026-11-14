using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.StaticData.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.UI.Game
{
    public class ScoreDisplayer : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _meter;
        [SerializeField] private Image _meterFill;

        private ILevelService _levels;
        private int _target;
        
        private ILevelLocalProgressService _localProgressService;
        
        [Inject]
        public void Constructor(ILevelLocalProgressService localProgressService, ILevelService levels)
        {
            _localProgressService = localProgressService;
            _levels = levels;
        }
        
        public void Initialize()
        {
            LevelConfig config = _levels.GetCurrentLevelStaticData().LevelConfig;
            _target = config.GetZumaTarget();
            _meter.gameObject.SetActive(_levels.Mode != ZumaMode.Survival);
            
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
            string mode = _levels.Mode == ZumaMode.Adventure
                ? $"Lives {_levels.AdventureLives}"
                : $"{_levels.Mode} · {_levels.GetGauntletRank(score)}";
            _levelText.text = $"Score {score}  ·  {mode}";
            float progress = Mathf.Clamp01((float)score / _target);
            _meterFill.rectTransform.anchorMax = new Vector2(progress, 1f);
            _meterFill.color = progress >= 1f ? new Color(0.3f, 0.75f, 0.3f) : new Color(0.95f, 0.53f, 0.12f);
        }
    }
}