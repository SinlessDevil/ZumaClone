using System;
using System.Collections.Generic;
using Code.Services.Input.Device;
using Code.Services.StaticData;
using Code.StaticData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Code.Services.Input
{
    public class InputService : IInputService, ITickable
    {
        private readonly GameplayInputStaticData _config;
        private readonly Func<Vector2, int, bool> _allowsPointer;
        private readonly List<RaycastResult> _uiHits = new();
        private IInputDevice _inputDevice;
        private Rect _inputArea;
        private EventSystem _eventSystem;
        private PointerEventData _pointer;
        
        public Vector2 Direction => _inputDevice?.Direction ?? Vector2.zero;
        public Vector3 TouchPositionToWorldPosition => _inputDevice?.TouchPositionToWorldPosition ?? Vector3.zero;
        public Vector3 TouchPosition => _inputDevice?.TouchPosition ?? Vector3.zero;
        public bool IsActiveInput => _inputDevice is not null and not NullableInputDevice;
        public bool CanAim => Time.timeScale > 0f && (_inputDevice?.CanAim ?? false) &&
                              _inputArea.Contains(TouchPosition);
        public bool UsesMouse => _inputDevice is MouseInputDevice;

        public event Action<Vector2> TapEvent;
        public event Action<Vector2> AlternatePointerEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;
        public event Action InputUpdateEvent;

        public InputService(IStaticDataService staticData)
        {
            _config = staticData.GameplayInputConfig;
            _allowsPointer = AllowsPointer;
        }

        public void SetInputDevice(IInputDevice inputDevice)
        {
            if (_inputDevice != null)
            {
                UnsubscribeInputEvents(_inputDevice);
            }
            
            _inputDevice = inputDevice ?? throw new ArgumentNullException(nameof(inputDevice));
            
            SubscribeInputEvents(_inputDevice);
        }

        public void Tick()
        {
            if (_inputDevice == null)
                return;

            if (Time.timeScale == 0f || !Application.isFocused)
            {
                _inputDevice.CancelGesture();
                return;
            }

            Rect screen = new(0f, 0f, Screen.width, Screen.height);
            _inputArea = _config.GetGameplayInputArea(screen, Screen.safeArea);
            _inputDevice.UpdateInput(_allowsPointer);

            if (CanAim && Direction.sqrMagnitude > 0f)
            {
                InputUpdateEvent?.Invoke();
            }
        }

        public void Cleanup()
        {
            SetInputDevice(new NullableInputDevice());
        }

        private bool AllowsPointer(Vector2 position, int pointerId)
        {
            if (!_inputArea.Contains(position))
                return false;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return true;

            if (_eventSystem != eventSystem)
            {
                _eventSystem = eventSystem;
                _pointer = new PointerEventData(eventSystem);
            }

            _pointer.Reset();
            _pointer.position = position;
            _pointer.pointerId = pointerId;
            _eventSystem.RaycastAll(_pointer, _uiHits);
            bool blocked = _uiHits.Exists(IsInteractiveUi);
            _uiHits.Clear();
            return !blocked;
        }

        private static bool IsInteractiveUi(RaycastResult hit) => hit.module is GraphicRaycaster &&
            (ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject) != null ||
             ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit.gameObject) != null ||
             ExecuteEvents.GetEventHandler<IDragHandler>(hit.gameObject) != null);

        private void SubscribeInputEvents(IInputDevice inputDevice)
        {
            inputDevice.TapEvent += OnTap;
            inputDevice.AlternatePointerEvent += OnAlternatePointer;
            inputDevice.PointerDownEvent += OnPointerDown;
            inputDevice.PointerUpEvent += OnPointerUp;
        }

        private void UnsubscribeInputEvents(IInputDevice inputDevice)
        {
            inputDevice.TapEvent -= OnTap;
            inputDevice.AlternatePointerEvent -= OnAlternatePointer;
            inputDevice.PointerDownEvent -= OnPointerDown;
            inputDevice.PointerUpEvent -= OnPointerUp;
        }

        private void OnTap(Vector2 position) => TapEvent?.Invoke(position);

        private void OnAlternatePointer(Vector2 position) => AlternatePointerEvent?.Invoke(position);
        
        private void OnPointerDown() => PointerDownEvent?.Invoke();
        
        private void OnPointerUp() => PointerUpEvent?.Invoke();
    }
}
