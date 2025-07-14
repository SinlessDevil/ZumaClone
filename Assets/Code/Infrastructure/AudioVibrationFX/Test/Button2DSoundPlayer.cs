using Code.Infrastructure.AudioVibrationFX.Services.Sound;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Infrastructure.AudioVibrationFX.Test
{
    [ExecuteAlways]
    public class Button2DSoundPlayer : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _text;
        [SerializeField] private Sound2DType _soundType;

        private ISoundService _soundService;

        private void OnValidate()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_text == null)
                _text = GetComponent<Text>();

            UpdateTextInEditor();
        }

        [Inject]
        private void Construct(ISoundService soundService) => _soundService = soundService;

        private void Start() => _button.onClick.AddListener(OnPlaySound);

        private void OnDestroy() => _button.onClick.RemoveListener(OnPlaySound);

        private void OnPlaySound() => _soundService.PlaySound(_soundType);

        private void UpdateTextInEditor()
        {
            if (!Application.isPlaying && _text != null)
                _text.text = _soundType.ToString();
        }
    }
}