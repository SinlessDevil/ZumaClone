using Code.Infrastructure.AudioVibrationFX.Services.Music;
using Code.Infrastructure.AudioVibrationFX.Services.Sound;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Infrastructure.AudioVibrationFX.Test
{
    public class MusicTestButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private MusicType _musicType;

        private IMusicService _musicService;
        private MusicState _state = MusicState.Stopped;

        [Inject]
        private void Construct(IMusicService musicService)
        {
            _musicService = musicService;
        }

        private void OnValidate()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_label == null)
                _label = GetComponentInChildren<Text>();
        }

        private void Start()
        {
            _button.onClick.AddListener(OnButtonClick);
            UpdateLabel();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            switch (_state)
            {
                case MusicState.Stopped:
                    _musicService.Play(_musicType);
                    _state = MusicState.Playing;
                    break;

                case MusicState.Playing:
                    _musicService.Pause();
                    _state = MusicState.Paused;
                    break;

                case MusicState.Paused:
                    _musicService.Resume();
                    _state = MusicState.PlayingPaused;
                    break;

                case MusicState.PlayingPaused:
                    _musicService.Stop();
                    _state = MusicState.Stopped;
                    break;
            }

            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (_label == null) return;

            switch (_state)
            {
                case MusicState.Stopped: _label.text = $"▶ Play {_musicType}"; break;
                case MusicState.Playing: _label.text = $"⏸ Pause {_musicType}"; break;
                case MusicState.Paused: _label.text = $"▶ Resume {_musicType}"; break;
                case MusicState.PlayingPaused: _label.text = $"⏹ Stop {_musicType}"; break;
            }
        }

        private enum MusicState
        {
            Stopped,
            Playing,
            Paused,
            PlayingPaused
        }
    }
}