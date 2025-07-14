using System.Collections.Generic;
using Code.Infrastructure.AudioVibrationFX.Services.Sound;
using Code.Infrastructure.AudioVibrationFX.Services.StaticData;
using Code.Infrastructure.AudioVibrationFX.StaticData;
using UnityEngine;

namespace Code.Infrastructure.AudioVibrationFX.Services.Music
{
    public class MusicService : IMusicService
    {
        private readonly IAudioVibrationStaticDataService _audioVibrationStaticDataService;
        private readonly Dictionary<MusicType, SoundData> _cachedMusic = new();
        
        private AudioSource _musicSource;

        public MusicService(IAudioVibrationStaticDataService staticDataService)
        {
            _audioVibrationStaticDataService = staticDataService;
        }

        public void CreateMusicRoot()
        {
            var go = new GameObject("[MusicPlayer]");
            Object.DontDestroyOnLoad(go);
            _musicSource = go.AddComponent<AudioSource>();
        }

        public void CacheMusic()
        {
            foreach (var music in _audioVibrationStaticDataService.SoundsData.MusicData)
            {
                if (!_cachedMusic.ContainsKey(music.MusicType))
                    _cachedMusic.Add(music.MusicType, music);
            }
        }

        public void Play(MusicType type, bool loop = true)
        {
            if (!_cachedMusic.TryGetValue(type, out var data))
            {
                Debug.LogWarning($"[MusicService] No music found for type: {type}");
                return;
            }

            if (_musicSource.isPlaying)
                _musicSource.Stop();

            _musicSource.clip = data.Clip;
            _musicSource.volume = data.Volume;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        public void Pause() => _musicSource.Pause();

        public void Resume()
        {
            if (_musicSource.clip != null)
                _musicSource.UnPause();
        }

        public void Stop() => _musicSource.Stop();
        
        public void SetVolume(float volume)
        {
            _musicSource.volume = Mathf.Clamp01(volume);
        }

        public float GetVolume()
        {
            return _musicSource.volume;
        }
    }
}
