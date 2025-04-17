using Code.Infrastructure.StateMachine.MonoBehaviours;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Buttons
{
    public abstract class LoadStateButton<TState> : MonoBehaviour where TState : class
    {
        [SerializeField] private Button _button;
        [SerializeField] private StateLoader<TState> _stateLoader;

        private void OnEnable() => _button.onClick.AddListener(LoadState);

        private void OnDisable() => _button.onClick.RemoveListener(LoadState);

        private void LoadState()
        {
            LoadState(_stateLoader);    
        }

        protected abstract void LoadState(StateLoader<TState> stateMachine);   
    }
}