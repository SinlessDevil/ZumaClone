using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Infrastructure.AudioVibrationFX.StaticData
{
    [CreateAssetMenu(fileName = "SoundData", menuName = "StaticData/SoundsData")]
    public class SoundsData : SerializedScriptableObject
    { 
        public List<SoundData> Sounds2DData = new();
        public List<Sound3DData> Sounds3DData = new();
        public List<SoundData> MusicData = new();
    }
}