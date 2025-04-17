using System.Collections.Generic;
using System.Linq;
using Code.Services.PersistenceProgress;
using UnityEngine;
using Zenject;

namespace Code.Services.SFX
{
    public class SoundService : MonoBehaviour, ISoundService
    {
        [SerializeField] private AudioClip _buttonClickClip;
        
        private bool _isSound => _progressService.PlayerData.PlayerSettings.Sound;
        private List<AudioSource> _sourcesList = new();
        private AudioSource _loopSource;
        private IPersistenceProgressService _progressService;

        [Inject]
        public void Constructor(IPersistenceProgressService progressService)
        {
            _progressService = progressService;
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _sourcesList = gameObject.GetComponents<AudioSource>().ToList();
            var first = _sourcesList[_sourcesList.Count - 1];
            _loopSource = first;
            _sourcesList.Remove(first);
        }
        

        public void ButtonClick() => 
            TryPlayClip(_buttonClickClip);
        
        
        private void TryPlayClip(AudioClip clip)
        {
            if (_isSound)
            {
                var source = _sourcesList.Find(p => !p.isPlaying);
                if (!source)
                    return;
               
                source.clip = clip;
                source.Play();
            }
        }
        
        private void TryPlayClipForce(AudioClip clip)
        {
            if (_isSound)
            {
                var source = _sourcesList.Last();
                if (!source)
                    return;
                
                source.clip = clip;
                source.Play();
            }
        }
    }
}