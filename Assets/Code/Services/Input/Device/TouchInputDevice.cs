using System;
using Code.UI;
using UnityEngine;

namespace Code.Services.Input.Device
{
    public class TouchInputDevice : IInputDevice
    {
        private int? _aimingFinger;

        public Vector2 Direction { get; private set; }
        public Vector3 TouchPositionToWorldPosition { get; private set; }
        public Vector3 TouchPosition { get; private set; }
        public bool CanAim => _aimingFinger.HasValue;

        public event Action<Vector2> TapEvent;
        public event Action<Vector2> AlternatePointerEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;

        public void UpdateInput()
        {
            int count = UnityEngine.Input.touchCount;
            if (count == 0)
            {
                CancelGesture();
                return;
            }

            int? aimingFingerAtStart = _aimingFinger;
            for (int i = 0; i < count; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                if (touch.fingerId == _aimingFinger)
                {
                    UpdateAimingFinger(touch);
                }
                else if (touch.phase == TouchPhase.Began && InputZona.Contains(touch.position, touch.fingerId))
                {
                    if (aimingFingerAtStart.HasValue || _aimingFinger.HasValue)
                        AlternatePointerEvent?.Invoke(touch.position);
                    else
                        BeginAim(touch);
                }
            }
        }

        public void CancelGesture()
        {
            _aimingFinger = null;
            Direction = Vector2.zero;
        }

        private void BeginAim(Touch touch)
        {
            _aimingFinger = touch.fingerId;
            TouchPosition = touch.position;
            PointerDownEvent?.Invoke();
        }

        private void UpdateAimingFinger(Touch touch)
        {
            Direction = touch.position - (Vector2)TouchPosition;
            TouchPosition = touch.position;
            Camera camera = Camera.main;
            if (camera != null)
                TouchPositionToWorldPosition = camera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10f));

            if (touch.phase == TouchPhase.Ended)
            {
                if (InputZona.Contains(touch.position, touch.fingerId))
                    TapEvent?.Invoke(touch.position);
                PointerUpEvent?.Invoke();
                CancelGesture();
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                CancelGesture();
            }
        }
    }
}
