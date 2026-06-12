using System.Collections.Generic;
using System.Linq;
using Code.Services.Factories.UIFactory;
using Code.UI;
using UnityEngine;

namespace Code.Services.Providers.Widgets
{
    public class WidgetProvider : IWidgetProvider
    {
        private const int InitialCount = 10;
        
        private List<Widget> _pool;
        
        private readonly IUIFactory _uiFactory;

        public WidgetProvider(IUIFactory uiFactory)
        {
            _uiFactory = uiFactory;
        }
        
        public void CreatePoolWidgets()
        {
            _pool = new List<Widget>();

            for (int i = 0; i < InitialCount; i++)
            {
                var widget = CreateObject();
                widget.Deactivate();
            }
        }
        
        public void CleanupPool()
        {
            foreach (var ball in _pool.Where(ball => ball != null))
            {
                Object.Destroy(ball.gameObject);
            }

            _pool.Clear();
        }

        public Widget GetWidget(Vector3 worldPosition)
        {
            foreach (var widget in _pool)
            {
                if (!widget.gameObject.activeInHierarchy)
                {
                    widget.Activate(worldPosition);
                    return widget;
                }
            }

            Widget newWidget = CreateObject();
            newWidget.Activate(worldPosition);
            return newWidget;
        }

        public void ReturnWidget(Widget widget)
        {
            widget.Deactivate();
        }
        
        private Widget CreateObject()
        {
            var createdObject = _uiFactory.CreateWidget();
            _pool.Add(createdObject);
            return createdObject;
        }
    }   
}