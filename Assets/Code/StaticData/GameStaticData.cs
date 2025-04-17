using System;
using UnityEngine;

namespace Code.StaticData
{
    [CreateAssetMenu(menuName = "StaticData/Game", fileName = "GameConfig", order = 0)]
    public class GameStaticData : ScriptableObject
    {
        [Space(10)] [Header("Scenes")]
        public string InitialScene = "Initial";
        public string GameScene = "Game";
        public string MenuScene = "Menu";
        public bool CanLoadCurrentOpenedScene = false;
        [Space(10)] [Header("Input")]
        public TypeInput TypeInput = TypeInput.PC;
        [Space(10)] [Header("FPS")]
        public float TargetFPS = 60;
        [Space(10)] [Header("Log Stack Trace")]
        public LogStackTrace LogStackTraceDev = new LogStackTrace();
        public LogStackTrace LogStackTraceRelease = new LogStackTrace();
        [Space(10)] [Header("Debug")]
        public bool DebugMode = true;
    }

    [Serializable]
    public class LogStackTrace
    {
        public StackTraceLogType Info = StackTraceLogType.None;
        public StackTraceLogType Errors = StackTraceLogType.ScriptOnly;
    }

    [Serializable]
    public enum TypeInput
    {
        PC,
        Mobile
    }
}