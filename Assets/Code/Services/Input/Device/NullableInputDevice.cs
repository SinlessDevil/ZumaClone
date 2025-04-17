using System;
using UnityEngine;

namespace Code.Services.Input.Device
{
    public class NullableInputDevice : IInputDevice
    {
        public Vector2 Direction { get; set; }
        public Vector3 TouchPositionToWorldPosition { get; }
        public Vector3 TouchPosition { get; }

        public event Action<Vector2> TapEvent;
        public event Action PointerDownEvent;
        public event Action PointerUpEvent;
        
        public void UpdateInput()
        {
            return;
        }
    }
}