using System;
using Code.Services.Input.Device;
using UnityEngine;

namespace Code.Services.Input
{
    public interface IInputService
    {
        public event Action InputUpdateEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;
        
        public Vector2 Direction { get; }
        public Vector3 TouchPositionToWorldPosition { get; }
        public bool IsActiveInput { get; }
        Vector3 TouchPosition { get; }

        void SetInputDevice(IInputDevice inputDevice);
        void Cleanup();
    }   
}