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

            Vector3 touchScreenPosition = inputService.TouchPosition;
            touchScreenPosition.z = Camera.main.WorldToScreenPoint(worldPosition).z;

            Vector3 touchWorldPosition = Camera.main.ScreenToWorldPoint(touchScreenPosition);
            return Vector3.Distance(worldPosition, touchWorldPosition);
        }
    }
}