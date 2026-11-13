using Code.Services.Levels;
using Code.Services.LocalProgress;
using Code.StaticData.Levels;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.UI.Game
{
    public class ScoreDisplayer : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;

        private ILevelService _levels;
        private RectTransform _meterFill;
        private UnityEngine.UI.Image _meterColor;
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
            _levelText.raycastTarget = false;
            if (_levels.Mode != ZumaMode.Survival)
                CreateMeter();
            
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
            if (_meterFill == null)
                return;

            float progress = Mathf.Clamp01((float)score / _target);
            _meterFill.anchorMax = new Vector2(progress, 1f);
            _meterColor.color = progress >= 1f ? new Color(0.3f, 0.75f, 0.3f) : new Color(0.95f, 0.53f, 0.12f);
        }

        private void CreateMeter()
        {
            GameObject bar = new("ZumaMeter", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform rect = (RectTransform)bar.transform;
            rect.SetParent(_levelText.rectTransform, false);
            rect.anchorMin = new Vector2(0.05f, 0f);
            rect.anchorMax = new Vector2(0.95f, 0f);
            rect.offsetMin = new Vector2(0f, -9f);
            rect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image background = bar.GetComponent<UnityEngine.UI.Image>();
            background.raycastTarget = false;
            background.color = new Color(0.16f, 0.11f, 0.07f, 0.9f);

            GameObject fill = new("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            _meterFill = (RectTransform)fill.transform;
            _meterFill.SetParent(rect, false);
            _meterFill.anchorMin = Vector2.zero;
            _meterFill.offsetMin = Vector2.zero;
            _meterFill.offsetMax = Vector2.zero;
            _meterColor = fill.GetComponent<UnityEngine.UI.Image>();
            _meterColor.raycastTarget = false;
        }
    }
}