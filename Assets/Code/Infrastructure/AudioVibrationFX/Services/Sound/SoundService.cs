using System.Collections.Generic;
using Code.Infrastructure.AudioVibrationFX.Services.StaticData;
using Code.Infrastructure.AudioVibrationFX.StaticData;
using UnityEngine;

namespace Code.Infrastructure.AudioVibrationFX.Services.Sound
{
    public class SoundService : ISoundService
    {
        private readonly IAudioVibrationStaticDataService _audioVibrationStaticDataService;

        private readonly List<AudioSource> _2dAudioPool = new();
        private readonly List<AudioSource> _3dAudioPool = new();

        private readonly Dictionary<Sound2DType, SoundData> _cached2DSounds = new();

        private const int PoolSize = 5;

        private Transform _poolParent2D;
        private Transform _poolParent3D;

        private float _globalVolume = 1f;
        
        public SoundService(IAudioVibrationStaticDataService audioVibrationStaticDataService)
        {
            _audioVibrationStaticDataService = audioVibrationStaticDataService;
        }

        public void Cache2DSounds()
        {
            foreach (var sound in _audioVibrationStaticDataService.SoundsData.Sounds2DData)
            {
                if (!_cached2DSounds.ContainsKey(sound.Sound2DType))
                    _cached2DSounds.Add(sound.Sound2DType, sound);
            }
        }
        
        public void CreateSoundsPool()
        {
            _poolParent2D = CreatePoolParent("[Audio2D Pool]");
            _poolParent3D = CreatePoolParent("[Audio3D Pool]");

            CreateAudioPool(_2dAudioPool, _poolParent2D, "Audio2D", PoolSize);
            CreateAudioPool(_3dAudioPool, _poolParent3D, "Audio3D", PoolSize);
        }

        public void PlaySound(Sound2DType type)
        {
            if (!_cached2DSounds.TryGetValue(type, out var data))
            {
                Debug.LogWarning($"[SoundService] No sound data found for 2D sound type: {type}");
                return;
            }
            
            if (!TryGetAvailableSource(_2dAudioPool, out var source, _poolParent2D, "Audio2D"))
            {
                Debug.LogWarning("[SoundService] Could not create new AudioSource in 2D pool");
                return;
            }
            
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

            if (!TryGetAvailableSource(_3dAudioPool, out var source, _poolParent3D, "Audio3D"))
            {
                Debug.LogWarning("[SoundService] Could not create new AudioSource in 3D pool");
                return;
            }

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
        
        private Transform CreatePoolParent(string name)
        {
            var parent = new GameObject(name).transform;
            Object.DontDestroyOnLoad(parent);
            return parent;
        }

        private void CreateAudioPool(List<AudioSource> pool, Transform parent, string prefix, int size)
        {
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject($"{prefix}_{i}");
                go.transform.SetParent(parent);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                pool.Add(source);
            }
        }
        
        private bool TryGetAvailableSource(List<AudioSource> pool, out AudioSource source, Transform parent = null, 
            string prefix = "Dynamic")
        {
            source = pool.Find(s => !s.isPlaying);
    
            if (source != null)
                return true;
            
            int index = pool.Count;
            var go = new GameObject($"{prefix}_{index}");
            if (parent != null)
                go.transform.SetParent(parent);

            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            pool.Add(source);

            Debug.LogWarning($"[SoundService] Pool expanded: {prefix}_{index}");

            return true;
        }
        
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
