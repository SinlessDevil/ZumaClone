using System.Collections.Generic;
using Code.Services.SFX.StaticData;
using Code.StaticData.SFX;
using UnityEngine;

namespace Code.Services.SFX.Sound
{
    public class SoundService : ISoundService
    {
        private readonly IAudioVibrationStaticDataService _audioVibrationStaticDataService;

        private readonly List<AudioSource> _2dAudioPool;
        private readonly List<AudioSource> _3dAudioPool;

        private readonly Dictionary<Sound2DType, SoundData> _cached2DSounds = new();

        private float _globalVolume = 1f;

        public SoundService(IAudioVibrationStaticDataService audioVibrationStaticDataService,
            AudioSource[] soundSources2D, AudioSource[] soundSources3D)
        {
            _audioVibrationStaticDataService = audioVibrationStaticDataService;
            _2dAudioPool = new List<AudioSource>(soundSources2D);
            _3dAudioPool = new List<AudioSource>(soundSources3D);
        }

        public void Cache2DSounds()
        {
            foreach (var sound in _audioVibrationStaticDataService.SoundsData.Sounds2DData)
            {
                if (!_cached2DSounds.ContainsKey(sound.Sound2DType))
                    _cached2DSounds.Add(sound.Sound2DType, sound);
            }
        }
        
        public void PlaySound(Sound2DType type)
        {
            if (!_cached2DSounds.TryGetValue(type, out var data))
            {
                Debug.LogWarning($"[SoundService] No sound data found for 2D sound type: {type}");
                return;
            }
            
            AudioSource source = _2dAudioPool.Find(candidate => !candidate.isPlaying);
            if (source == null)
                return;

            SetupSource(source, data);
            source.Play();
        }
        
        public void PlaySound(Sound3DType type, Vector3 position)
        {
            var soundDataList = _audioVibrationStaticDataService.SoundsData.Sounds3DData;
            var data = soundDataList.Find(s => s.Sound3DType == type);

            if (data == null)
            {
                Debug.LogWarning($"[SoundService] No sound data found for 3D sound type: {type}");
                return;
            }

            AudioSource source = _3dAudioPool.Find(candidate => !candidate.isPlaying);
            if (source == null)
                return;

            Setup3DSource(source, data, position);
            source.Play();
        }

        public void SetGlobalVolume(float volume)
        {
            _globalVolume = Mathf.Clamp01(volume);

            foreach (var src in _2dAudioPool)
                src.volume = _globalVolume;

            foreach (var src in _3dAudioPool)
                src.volume = _globalVolume;
        }

        public float GetGlobalVolume() => _globalVolume;
        
        private void SetupSource(AudioSource source, SoundData data)
        {
            source.clip = data.Clip;
            source.volume = data.Volume * _globalVolume;
            source.loop = data.Loop;
            source.playOnAwake = data.PlayOnAwake;
        }
        
        private void Setup3DSource(AudioSource source, Sound3DData data, Vector3 position)
        {
            source.transform.position = position;
    
            SetupSource(source, data);

            source.spatialBlend = data.SpatialBlend;
            source.rolloffMode = data.RolloffMode;
            source.minDistance = data.MinDistance;
            source.maxDistance = data.MaxDistance;
        }
    }
}
