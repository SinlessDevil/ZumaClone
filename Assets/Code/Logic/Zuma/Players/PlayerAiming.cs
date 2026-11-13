using Code.Extensions;
using Code.Services.Factories.UIFactory;
using Code.Services.Input;
using UnityEngine;
using Zenject;

namespace Code.Logic.Zuma.Players
{
    public class PlayerAiming : MonoBehaviour
    {
        private Transform _rotationTransform;
        private IInputService _inputService;

        [Inject]
        public void Constructor(IInputService inputService, IUIFactory uiFactory)
        {
            _inputService = inputService;
        }

        public void Initialize(Transform rotationTransform) => _rotationTransform = rotationTransform;

        private void LateUpdate()
        {
            if (_rotationTransform == null || !_inputService.CanAim)
                return;

            Vector3 target = _inputService.GetWorldPointerPosition(_rotationTransform.position);
            Vector3 direction = target - _rotationTransform.position;
            if (direction.sqrMagnitude > 0.0001f)
                _rotationTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
