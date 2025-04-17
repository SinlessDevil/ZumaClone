using UnityEngine;

namespace Code.StaticData
{
    [CreateAssetMenu(menuName = "StaticData/BuildSettings", fileName = "BuildSettings", order = 0)]
    public class BuildSettings : ScriptableObject
    {
        public bool MakeBuild = false;
    }
}