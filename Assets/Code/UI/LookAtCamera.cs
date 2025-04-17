using UnityEngine;

namespace Code.UI
{
    public class LookAtCamera : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Update()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        
            Vector3 direction = _mainCamera.transform.position - transform.position;
            Quaternion rotation = Quaternion.LookRotation(direction);
            transform.rotation = rotation;
        }
    }   
}