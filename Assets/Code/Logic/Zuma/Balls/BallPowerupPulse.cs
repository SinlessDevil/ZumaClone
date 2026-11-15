using UnityEngine;

namespace Code.Logic.Zuma.Balls
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Ball))]
    public sealed class BallPowerupPulse : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _cyclesPerSecond = 1.8f;
        [SerializeField, Min(0f)] private float _peakEmission = 1.25f;

        private Ball _ball;
        private float _startedAt;

        private void Awake() => _ball = GetComponent<Ball>();

        public void SetPulsing(bool active)
        {
            enabled = active;
            if (!active)
                return;

            _startedAt = Time.time;
            _ball.SetPowerupHighlight(1f, _peakEmission);
        }

        private void Update()
        {
            float phase = (Time.time - _startedAt) * _cyclesPerSecond * (2f * Mathf.PI);
            float pulse = (1f + Mathf.Cos(phase)) * 0.5f;
            _ball.SetPowerupHighlight(pulse, _peakEmission);
        }
    }
}
