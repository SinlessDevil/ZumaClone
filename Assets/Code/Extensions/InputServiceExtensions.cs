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

        public static Vector3 GetWorldPointerPosition(this IInputService inputService, Vector3 worldPosition) => ScreenToField(inputService.TouchPosition, worldPosition);

        public static Vector3 ScreenToField(Vector2 screenPosition, Vector3 worldPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return worldPosition;

            Ray ray = camera.ScreenPointToRay(screenPosition);
            Plane field = new(Vector3.up, worldPosition);
            return field.Raycast(ray, out float distance) ? ray.GetPoint(distance) : worldPosition;
        }
    }
}