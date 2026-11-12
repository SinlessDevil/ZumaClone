using Code.Services.Input;
using UnityEngine;

namespace Code.Extensions
{
    public static class InputServiceExtensions
    {
        public static float GetWorldDistanceTo(this IInputService inputService, Vector3 worldPosition)
        {
            if (Camera.main == null)
            {
                Debug.LogError("Main Camera not found!");
                return float.MaxValue;
            }

            Vector3 touchWorldPosition = inputService.GetWorldPointerPosition(worldPosition);
            return Vector3.Distance(worldPosition, touchWorldPosition);
        }

        public static Vector3 GetWorldPointerPosition(this IInputService inputService, Vector3 worldPosition)
        {
            Camera camera = Camera.main;
            Vector3 screenPosition = inputService.TouchPosition;
            screenPosition.z = camera.WorldToScreenPoint(worldPosition).z;
            return camera.ScreenToWorldPoint(screenPosition);
        }
    }
}