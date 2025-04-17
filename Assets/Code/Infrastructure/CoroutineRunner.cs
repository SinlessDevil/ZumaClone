using UnityEngine;

namespace Code.Infrastructure
{
    public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}