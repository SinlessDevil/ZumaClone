using System;
using Code.Services.Input.Device;
using UnityEngine;
using Zenject;

namespace Code.Services.Input
{
    public class InputService : IInputService, ITickable
    {
        private IInputDevice _inputDevice;
        
        public Vector2 Direction => _inputDevice?.Direction ?? Vector2.zero;
        public Vector3 TouchPositionToWorldPosition => _inputDevice?.TouchPositionToWorldPosition ?? Vector3.zero;
        public Vector3 TouchPosition => _inputDevice?.TouchPosition ?? Vector3.zero;
        public bool IsActiveInput => _inputDevice != null;

        public event Action<Vector2> TapEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;
        public event Action InputUpdateEvent;

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

            _inputDevice.UpdateInput();
            
            if (Direction.magnitude > 0)
            {
                InputUpdateEvent?.Invoke();
            }
        }

        public void Cleanup()
        {
            SetInputDevice(new NullableInputDevice());
        }

        private void SubscribeInputEvents(IInputDevice inputDevice)
        {
            inputDevice.TapEvent += OnTap;
            inputDevice.PointerDownEvent += OnPointerDown;
            inputDevice.PointerUpEvent += OnPointerUp;
        }

        private void UnsubscribeInputEvents(IInputDevice inputDevice)
        {
            inputDevice.TapEvent -= OnTap;
            inputDevice.PointerDownEvent -= OnPointerDown;
            inputDevice.PointerUpEvent -= OnPointerUp;
        }

        private void OnTap(Vector2 position) => TapEvent?.Invoke(position);
        
        private void OnPointerDown() => PointerDownEvent?.Invoke();
        
        private void OnPointerUp() => PointerUpEvent?.Invoke();
    }
}
