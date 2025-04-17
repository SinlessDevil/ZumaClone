using UnityEngine;

namespace Code.UI.Menu.Windows
{
    public abstract class BaseWindow : MonoBehaviour
    {
        [SerializeField] private TypeWindow _typeWindow;
        
        public TypeWindow TypeWindow => _typeWindow;

        public abstract void Initialize();
    }
}