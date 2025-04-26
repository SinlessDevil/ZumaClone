using UnityEngine;

namespace Plugins.CI.Editor
{
    [CreateAssetMenu(menuName = "CI Config", fileName = "CIConfig", order = 0)]
    public class CIConfig : ScriptableObject
    {
        public bool IsAndroidBuild = true;
        public bool IsIosBuild;
        public string Version = "1.0.0";

        [Header("Android Keystore")]
        public bool UseCustomKeystore = false;
        public string KeystoreName;
        public string KeystorePass;
        public string KeyaliasName;
        public string KeyaliasPass;

        [Header("iOS Settings")]
        public bool UseAutomaticSigning = true;
        public string AppleDeveloperTeamID;
    }
}