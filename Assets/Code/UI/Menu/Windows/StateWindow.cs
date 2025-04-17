using System;
using System.Collections.Generic;
using Code.UI.Menu.Windows.Map;
using Code.UI.Menu.Windows.Meta;
using Code.UI.Menu.Windows.Shop;

namespace Code.UI.Menu.Windows
{
    public class StateWindow
    {
        private List<BaseWindow> _windows = new();
        private Dictionary<Type, IWindowState> _behaviorsMap;
        private IWindowState _behaviorCurrent;
        
        public StateWindow(List<BaseWindow> windows)
        {
            _windows = windows;
            
            InitBehaviors();
        }

        private void InitBehaviors()
        {
            this._behaviorsMap = new Dictionary<Type, IWindowState>
            {
                [typeof(MapWindowState)] = new MapWindowState(),
                [typeof(ShopWindowState)] = new ShopWindowState(),
                [typeof(MetaWindowState)] = new MetaWindowState(),
            };

            BindWindowsToStates();
        }

        private void BindWindowsToStates()
        {
            var stateToWindowMap = new Dictionary<Type, Type>
            {
                { typeof(MapWindowState), typeof(MapWindow) },
                { typeof(ShopWindowState), typeof(ShopWindow) },
                { typeof(MetaWindowState), typeof(MetaWindow) },
            };

            foreach (var entry in _behaviorsMap)
            {
                Type stateType = entry.Key;
                IWindowState stateInstance = entry.Value;

                if (stateToWindowMap.TryGetValue(stateType, out Type windowType))
                {
                    BaseWindow windowInstance = GetWindowByType(windowType);
                    if (windowInstance != null)
                    {
                        stateInstance.Constructor(windowInstance);
                    }
                }
            }
        }

        private BaseWindow GetWindowByType(Type windowType)
        {
            return this._windows.Find(window => window.GetType() == windowType);
        }
        
        public void Update()
        {
            _behaviorCurrent.Update();
        }
        
        private void SetBehavior(IWindowState newBehavior)
        {
            if (this._behaviorCurrent?.GetType() == newBehavior.GetType())
                return;
            
            this._behaviorCurrent?.Exit();
            this._behaviorCurrent = newBehavior;
            this._behaviorCurrent.Enter();
        }
        private IWindowState GetBehavior<T>() where T : IWindowState
        {
            var type = typeof(T);
            return this._behaviorsMap[type];
        }

        public void EnterMapWindow()
        {
            var behaviorByDefault = this.GetBehavior<MapWindowState>();
            this.SetBehavior(behaviorByDefault);
        }
        
        public void EnterShopWindow()
        {
            var behaviorByDefault = this.GetBehavior<ShopWindowState>();
            this.SetBehavior(behaviorByDefault);
        }
        
        public void EnterMetaWindow()
        {
            var behaviorByDefault = this.GetBehavior<MetaWindowState>();
            this.SetBehavior(behaviorByDefault);
        }
    }
}