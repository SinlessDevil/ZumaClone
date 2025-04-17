using Code.Extensions;
using Code.Services.Factories.UIFactory;
using Code.Services.Input;
using UnityEngine;
using Zenject;

namespace Code.Logic.Zuma.Players
{
    public class PlayerAiming : MonoBehaviour
    {
        private const float SwitchBallDistance = 0.5f;

        private Transform _rotationTransform;

        private float _rotationSpeed = 10f;
        private float _savedAngle = 0f;

        private bool _isAiming;

        private IInputService _inputService;
        private IUIFactory _uiFactory;

        [Inject]
        public void Constructor(
            IInputService inputService,
            IUIFactory uiFactory)
        {
            _inputService = inputService;
            _uiFactory = uiFactory;
        }

        private void Update()
        {
            if (_isAiming == false)
                return;

            if (!_uiFactory.GameHud.InputZona.IsActive)
                return;

            RotateObjectWithMouse();
        }

        public void Initialize(Transform rotationTransform)
        {
            _rotationTransform = rotationTransform;

            SubscribeEvents();
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _inputService.PointerDownEvent += OnEnableAiming;
            _inputService.PointerUpEvent += OnDisableAiming;
        }

        private void UnsubscribeEvents()
        {
            _inputService.PointerDownEvent -= OnEnableAiming;
            _inputService.PointerUpEvent -= OnDisableAiming;
        }

        private void OnEnableAiming()
        {
            float distanceToTouch = _inputService.GetWorldDistanceTo(transform.position);

            if (distanceToTouch >= SwitchBallDistance)
            {
                _savedAngle = _rotationTransform.eulerAngles.y;

                _isAiming = true;
            }
        }

        private void OnDisableAiming()
        {
            _isAiming = false;
        }

        private void RotateObjectWithMouse()
        {
            Vector3 mouseWorldPosition = _inputService.TouchPositionToWorldPosition;
            Vector3 direction = mouseWorldPosition - _rotationTransform.position;

            float newAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angleDifference = Mathf.DeltaAngle(_savedAngle, newAngle);
            float finalAngle = _savedAngle + angleDifference;

            float smoothedAngle = Mathf.LerpAngle(_rotationTransform.eulerAngles.y, finalAngle,
                Time.deltaTime * _rotationSpeed);

            _rotationTransform.rotation = Quaternion.Euler(0, smoothedAngle, 0);
        }
    }
}