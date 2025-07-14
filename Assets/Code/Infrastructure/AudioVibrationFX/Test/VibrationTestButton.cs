using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Code.Infrastructure.AudioVibrationFX.Services.Vibration;

namespace Code
{
    public class VibrationTestButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private VibrationType _vibrationType;

        private IVibrationService _vibrationService;

        [Inject]
        private void Construct(IVibrationService vibrationService)
        {
            _vibrationService = vibrationService;
        }

        private void OnValidate()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_label == null) _label = GetComponentInChildren<Text>();
        }

        private void Start()
        {
            _button.onClick.AddListener(OnClick);
            UpdateLabel();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            _vibrationService.Play(_vibrationType);
        }

        private void UpdateLabel()
        {
            if (_label != null)
                _label.text = $"▶ {_vibrationType}";
        }
    }   
}