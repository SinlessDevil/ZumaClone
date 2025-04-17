using System;
using UnityEngine;

namespace Code.Services.Input.Device
{
    public interface IInputDevice
    {
        Vector2 Direction { get; }
        Vector3 TouchPositionToWorldPosition { get; }
        Vector3 TouchPosition { get; }
        
        public event Action<Vector2> TapEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;

        public void UpdateInput();
    }
}