using Code.Services.PersistenceProgress;
using UnityEngine;
using Zenject;

namespace Code.Services.SFX
{
    public class MusicService : MonoBehaviour, IMusicService
    {
        [SerializeField] private AudioSource _audioSource;
        private bool _isMusic => _progressService.PlayerData.PlayerSettings.Music;
        
        private IPersistenceProgressService _progressService;

        [Inject]
        public void Constructor(IPersistenceProgressService progressService)
        {
            _progressService = progressService;
        }
        
        public void Update()
        {
            if (!_isMusic && _audioSource.isPlaying)
                _audioSource.Stop();
            
            if(_isMusic && !_audioSource.isPlaying)
                _audioSource.Play();
        }
    }
}