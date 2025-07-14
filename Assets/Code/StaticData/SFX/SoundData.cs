using System;
using Code.Services.SFX.Music;
using Code.Services.SFX.Sound;
using UnityEngine;

namespace Code.StaticData.SFX
{
    [Serializable]
    public class SoundData
    {
        public string Name;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume = 1f;
        public bool Loop = false;
        public bool PlayOnAwake = false;
        
        [HideInInspector] public Sound2DType Sound2DType = Sound2DType.Unknown;
        [HideInInspector] public Sound3DType Sound3DType = Sound3DType.Unknown;
        [HideInInspector] public MusicType MusicType = MusicType.Unknown;
    }

    [Serializable]
    public class Sound3DData : SoundData
    {
        [Range(0f, 1f)] public float SpatialBlend = 0f; // 0 = 2D, 1 = 3D
        
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Linear;
        public float MinDistance = 1f;
        public float MaxDistance = 500f;
    }
}