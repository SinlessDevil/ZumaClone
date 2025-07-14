using UnityEngine;
using Zenject;
using Code.Infrastructure.AudioVibrationFX.Services.Sound;

namespace Code.Infrastructure.AudioVibrationFX.Test
{
    public class TeleportingSoundEmitter : MonoBehaviour
    {
        [SerializeField] private float radius = 10f;
        [SerializeField] private float interval = 1f;
        [SerializeField] private Sound3DType soundType;

        private ISoundService _soundService;
        private float _timer;

        [Inject]
        private void Construct(ISoundService soundService)
        {
            _soundService = soundService;
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= interval)
            {
                _timer = 0f;
                MoveAndPlaySound();
            }
        }

        private void MoveAndPlaySound()
        {
            var cameraPos = Camera.main.transform.position;
            var randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Clamp(randomDir.y, -0.2f, 0.7f);

            var targetPos = cameraPos + randomDir * radius;
            transform.position = targetPos;

            _soundService.PlaySound(soundType, transform.position);
        }
    }   
}