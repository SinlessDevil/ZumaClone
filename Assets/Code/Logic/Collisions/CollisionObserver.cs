using System;
using UnityEngine;

namespace Code.Logic.Collisions
{
    public class CollisionObserver : MonoBehaviour
    {
        public event Action<Collision> ColliderEnter; 
        public event Action<Collision> ColliderExit;

        private void OnCollisionEnter(Collision collision) =>
            ColliderEnter?.Invoke(collision);

        private void OnCollisionExit(Collision collision) =>
            ColliderExit?.Invoke(collision);
    }
}
