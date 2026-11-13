using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.UI
{
    public class InputZona : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private static readonly List<RaycastResult> Hits = new();

        public static bool Contains(Vector2 position, int pointerId)
        {
            EventSystem system = EventSystem.current;
            if (system == null)
                return false;

            PointerEventData pointer = new(system) { position = position, pointerId = pointerId };
            system.RaycastAll(pointer, Hits);
            bool playable = false;
            foreach (RaycastResult hit in Hits)
            {
                if (hit.module is not GraphicRaycaster)
                    continue;

                playable = hit.gameObject.GetComponentInParent<InputZona>() != null;
                break;
            }

            Hits.Clear();
            return playable;
        }

        public bool IsActive { get; private set; }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            IsActive = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsActive = false;
        }
    }   
}