using UnityEngine;

namespace Code.Logic.Zuma.Players
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private Transform _rotationTransform;
        [SerializeField] private PlayerAiming _playerAiming;
        [SerializeField] private PlayerShooting _playerShooting;
        
        private void OnValidate()
        {
            if(_playerAiming == null)
                _playerAiming = GetComponent<PlayerAiming>();
            
            if(_playerShooting == null)
                _playerShooting = GetComponent<PlayerShooting>();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public PlayerShooting PlayerShooting => _playerShooting;
        
        public void Initialize()
        {
            _playerAiming.Initialize(_rotationTransform);
            _playerShooting.Initialize(_rotationTransform);
        }
        
        private void Dispose()
        {
            _playerShooting.Dispose();
        }
    }   
}