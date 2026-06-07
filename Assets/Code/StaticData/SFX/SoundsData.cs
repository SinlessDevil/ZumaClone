using System.Collections.Generic;
using UnityEngine;

namespace Code.StaticData.SFX
{
    [CreateAssetMenu(fileName = "SoundData", menuName = "StaticData/SoundsData")]
    public class SoundsData : ScriptableObject
    { 
        public List<SoundData> Sounds2DData = new();
        public List<Sound3DData> Sounds3DData = new();
        public List<SoundData> MusicData = new();
    }
}