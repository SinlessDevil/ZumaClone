using System;
using UnityEngine;

namespace Code.Services.Input.Device
{
    public class MouseInputDevice : IInputDevice
    {
        private bool _primaryPressed;
        public Vector2 Direction { get; private set; }
        public Vector3 TouchPositionToWorldPosition { get; private set; }
        public Vector3 TouchPosition { get; private set; }
        public bool CanAim { get; private set; }

        public event Action<Vector2> TapEvent;
        public event Action<Vector2> AlternatePointerEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;

        public void UpdateInput(Func<Vector2, int, bool> allowsPointer)
        {
            Vector3 position = UnityEngine.Input.mousePosition;
            Direction = position - TouchPosition;
            TouchPosition = position;
            CanAim = true;
            Camera camera = Camera.main;
            if (camera != null)
                TouchPositionToWorldPosition = camera.ScreenToWorldPoint(new Vector3(position.x, position.y, 10f));

            if (UnityEngine.Input.GetMouseButtonDown(1) && allowsPointer(position, -2))
                AlternatePointerEvent?.Invoke(position);

            if (UnityEngine.Input.GetMouseButtonDown(0) && allowsPointer(position, -1))
            {
                _primaryPressed = true;
                PointerDownEvent?.Invoke();
                TapEvent?.Invoke(position);
            }

            if (_primaryPressed && UnityEngine.Input.GetMouseButtonUp(0))
            {
                _primaryPressed = false;
                PointerUpEvent?.Invoke();
            }
        }

        public void CancelGesture()
        {
            CanAim = false;
            _primaryPressed = false;
        }
    }
}
