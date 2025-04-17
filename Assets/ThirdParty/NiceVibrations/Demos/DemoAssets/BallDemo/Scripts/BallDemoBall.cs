using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.NiceVibrations
{
    public class BallDemoBall : MonoBehaviour
    {
        public bool HapticsEnabled = true;
        public ParticleSystem HitParticles;
        public ParticleSystem HitPusherParticles;
        public LayerMask WallMask;
        public LayerMask PusherMask;
        public MMUIShaker LogoShaker;
        public AudioSource TransientAudioSource;

        protected Rigidbody2D _rigidBody;
        protected float _lastRaycastTimestamp = 0f;
        protected Animator _ballAnimator;
        protected int _hitAnimationParameter;

        protected virtual void Awake()
        {
            _rigidBody = this.gameObject.GetComponent<Rigidbody2D>();
            _ballAnimator = this.gameObject.GetComponent<Animator>();
            _hitAnimationParameter = Animator.StringToHash("Hit");
            MMNViOSCoreHaptics.CreateEngine();
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (WallMask == (WallMask | (1 << collision.gameObject.layer)))
            {
                HitWall();
            }
        }

        protected virtual void Update()
        {
            float raycastLength = 5f;

            Debug.DrawLine(this.transform.position, Vector3.down * raycastLength, Color.red);

            if (Time.time - _lastRaycastTimestamp > 1f)
            {
                _lastRaycastTimestamp = Time.time;
                RaycastHit2D hit = Physics2D.Raycast(this.transform.position, Vector2.down, raycastLength, WallMask);
                if (hit.collider != null)
                {
                    HitBottom();
                }
            }
        }

        protected virtual void HitBottom()
        {
            _rigidBody.AddForce(Vector2.up * 2500f);
            StartCoroutine(LogoShaker.Shake(0.2f));
        }

        protected virtual void HitWall()
        {
            float intensity = _rigidBody.velocity.magnitude / 100f;
            MMVibrationManager.TransientHaptic(intensity, 0.7f, true, this);
            TransientAudioSource.volume = intensity;
            StartCoroutine(LogoShaker.Shake(0.2f));
            TransientAudioSource.Play();
            _ballAnimator.SetTrigger(_hitAnimationParameter);
        }

        public virtual void HitPusher()
        {
            HitPusherParticles.Play();
            MMVibrationManager.TransientHaptic(0.85f, 0.05f, true, this);
            TransientAudioSource.volume = 0.1f;
            StartCoroutine(LogoShaker.Shake(0.2f));
            TransientAudioSource.Play();
            _ballAnimator.SetTrigger(_hitAnimationParameter);
        }
    }
}
