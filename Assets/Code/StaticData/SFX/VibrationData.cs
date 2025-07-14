using System;
using Code.Services.SFX.Vibration;
using MoreMountains.NiceVibrations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.StaticData.SFX
{
    [Serializable]
    public class VibrationData
    {
        public string Name;
        public VibrationMode Mode = VibrationMode.Preset;

        [ShowIf("Mode", VibrationMode.Preset)]
        public HapticTypes HapticType;

        [ShowIf("Mode", VibrationMode.Constant)]
        public float ConstantIntensity = 1f;
        [ShowIf("Mode", VibrationMode.Constant)]
        public float ConstantDuration = 0.3f;

        [ShowIf("Mode", VibrationMode.Emphasis)]
        public float EmphasisIntensity = 1f;
        [ShowIf("Mode", VibrationMode.Emphasis)]
        public float EmphasisSharpness = 1f;

        [ShowIf("Mode", VibrationMode.CustomCurve)]
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [ShowIf("Mode", VibrationMode.CustomCurve)]
        public float CurveDuration = 1f;

        [HideInInspector]
        public VibrationType VibrationType = VibrationType.Unknown;
    }
}