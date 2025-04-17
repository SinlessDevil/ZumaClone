using System;
using Code.Window;
using UnityEngine;

namespace Code.StaticData
{
    [Serializable]
    public class WindowConfig
    {
        public WindowTypeId WindowTypeId;
        public GameObject Prefab;
    }
}