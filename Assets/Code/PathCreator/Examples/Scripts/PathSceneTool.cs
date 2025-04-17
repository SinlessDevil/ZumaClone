using System;
using UnityEngine;

namespace PathCreation.Examples
{
    [ExecuteInEditMode]
    public abstract class PathSceneTool : MonoBehaviour
    {
        public bool autoUpdate = true;
        
        public PathCreator pathCreator;
        public event Action DestroyedEvent;

        protected VertexPath Path => pathCreator.path;

        public void TriggerUpdate() 
        {
            PathUpdated();
        }

        protected virtual void OnDestroy() 
        {
            if (DestroyedEvent != null) 
            {
                DestroyedEvent();
            }
        }

        protected abstract void PathUpdated();
    }
}
