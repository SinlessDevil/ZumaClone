using System.Collections.Generic;
using UnityEngine;

namespace Code.StaticData.SFX
{
    [CreateAssetMenu(fileName = "VibrationsData", menuName = "StaticData/VibrationsData")]
    public class VibrationsData : ScriptableObject
    {
        public List<VibrationData> Vibrations = new();
    }
}