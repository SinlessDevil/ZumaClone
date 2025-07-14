using Code.Infrastructure.AudioVibrationFX.Services.Sound;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Infrastructure.AudioVibrationFX.Test
{
    public class AudioVolumeTestUI : MonoBehaviour
    {
        [Header("Sound Volume")] 
        [SerializeField] private Slider _soundSlider;
        [SerializeField] private Text _soundValueText;

        [Header("Music Volume")] 
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Text _musicValueText;

        private ISoundService _soundService;
        private IMusicService _musicService;

        [Inject]
        private void Construct(ISoundService soundService, IMusicService musicService)
        {
            _soundService = soundService;
            _musicService = musicService;
        }

        private void Start()
        {
            _soundSlider.onValueChanged.AddListener(SetSoundVolume);
            _musicSlider.onValueChanged.AddListener(SetMusicVolume);

            _soundSlider.value = _soundService.GetGlobalVolume();
            _musicSlider.value = _musicService.GetVolume();

            UpdateText();
        }

        private void SetSoundVolume(float value)
        {
            _soundService.SetGlobalVolume(value);
            UpdateText();
        }

        private void SetMusicVolume(float value)
        {
            _musicService.SetVolume(value);
            UpdateText();
        }

        private void UpdateText()
        {
            _soundValueText.text = $"Sound: {Mathf.RoundToInt(_soundSlider.value * 100)}%";
            _musicValueText.text = $"Music: {Mathf.RoundToInt(_musicSlider.value * 100)}%";
        }

        private void OnDestroy()
        {
            _soundSlider.onValueChanged.RemoveAllListeners();
            _musicSlider.onValueChanged.RemoveAllListeners();
        }
    }
}