using System;
using UnityEngine;

namespace Code.Services.Input.Device
{
    public class MouseInputDevice : IInputDevice
    {
        private Vector2 _lastMousePosition;
        private Vector2 _currentMousePosition;
        
        public Vector2 Direction { get; private set; }
        public Vector3 TouchPositionToWorldPosition { get; private set; }
        public Vector3 TouchPosition { get; private set; }

        public event Action<Vector2> TapEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;

        public void UpdateInput()
        {
            _currentMousePosition = UnityEngine.Input.mousePosition;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                PointerDownEvent?.Invoke();
                _lastMousePosition = _currentMousePosition;
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                PointerUpEvent?.Invoke();
                TapEvent?.Invoke(_currentMousePosition);
            }

            if (UnityEngine.Input.GetMouseButton(0))
            {
                Direction = (_currentMousePosition - _lastMousePosition).normalized;
                _lastMousePosition = _currentMousePosition;
            }
            
            TouchPositionToWorldPosition = Camera.main.ScreenToWorldPoint(
                new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, 10f));

            TouchPosition = _currentMousePosition;
        }
    }
}