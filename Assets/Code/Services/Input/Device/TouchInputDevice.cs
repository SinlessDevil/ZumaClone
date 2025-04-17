using System;
using UnityEngine;

namespace Code.Services.Input.Device
{
    public class TouchInputDevice : IInputDevice
    {
        private Vector2 _lastTouchPosition;
        private Vector2 _currentTouchPosition;

        public Vector2 Direction { get; private set; }
        public Vector3 TouchPositionToWorldPosition { get; private set; }
        public Vector3 TouchPosition { get; private set; }

        public event Action<Vector2> TapEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;

        public void UpdateInput()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                _currentTouchPosition = touch.position;
                
                TouchPositionToWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(_currentTouchPosition.x, _currentTouchPosition.y, 10f));

                TouchPosition = _currentTouchPosition;
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        PointerDownEvent?.Invoke();
                        _lastTouchPosition = _currentTouchPosition;
                        break;

                    case TouchPhase.Moved:
                        Direction = _currentTouchPosition - _lastTouchPosition;
                        _lastTouchPosition = _currentTouchPosition;
                        break;

                    case TouchPhase.Ended:
                        PointerUpEvent?.Invoke();
                        TapEvent?.Invoke(_currentTouchPosition);
                        Direction = Vector2.zero;
                        break;
                }
            }
        }
    }
}