using System;
using Code.UI;
using UnityEngine;

namespace Code.Services.Input.Device
{
    public class MouseInputDevice : IInputDevice
    {
        public Vector2 Direction { get; private set; }
        public Vector3 TouchPositionToWorldPosition { get; private set; }
        public Vector3 TouchPosition { get; private set; }
        public bool CanAim { get; private set; }

        public event Action<Vector2> TapEvent;
        public event Action<Vector2> AlternatePointerEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;

        public void UpdateInput()
        {
            Vector3 position = UnityEngine.Input.mousePosition;
            Direction = position - TouchPosition;
            TouchPosition = position;
            CanAim = InputZona.Contains(position, -1);
            Camera camera = Camera.main;
            if (camera != null)
                TouchPositionToWorldPosition = camera.ScreenToWorldPoint(new Vector3(position.x, position.y, 10f));

            if (CanAim && UnityEngine.Input.GetMouseButtonDown(1))
                AlternatePointerEvent?.Invoke(position);

            if (CanAim && UnityEngine.Input.GetMouseButtonDown(0))
            {
                PointerDownEvent?.Invoke();
                TapEvent?.Invoke(position);
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
                PointerUpEvent?.Invoke();
        }

        public void CancelGesture() => CanAim = false;
    }
}
