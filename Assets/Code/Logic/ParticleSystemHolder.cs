using System.Collections.Generic;
using UnityEngine;

namespace Code.Logic
{
    public class ParticleSystemHolder : MonoBehaviour
    {
        [SerializeField] private List<ParticleSystem> _particleSystems = new();
        [SerializeField] private List<TrailRenderer> _trailRenderers = new();
        [SerializeField] private ParticleSystem _mainParticleSystem;

        public bool IsActive { get; private set; }
        
        public ParticleSystem MainParticleSystem => _mainParticleSystem;
        
        private void OnValidate()
        {
            if (_mainParticleSystem == null)
                _mainParticleSystem = GetComponent<ParticleSystem>();
            
            if (_particleSystems.Count == 0)
            {
                var particleSystemsInChildren = GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystemsInChildren)
                {
                    if (ps != _mainParticleSystem)
                    {
                        _particleSystems.Add(ps);
                    }
                }
            }
            
            if (_trailRenderers.Count == 0)
                _trailRenderers.AddRange(GetComponentsInChildren<TrailRenderer>());
        }
        
        public void Play()
        {
            IsActive = true;
            
            if(_particleSystems.Count != 0) 
                _particleSystems.ForEach(particleSystem => particleSystem.Play());
            
            if(_trailRenderers.Count != 0)
                _trailRenderers.ForEach(trailRenderer => trailRenderer.emitting = true);
        }

        public void Stop()
        {
            if(_particleSystems.Count != 0) 
                _particleSystems.ForEach(particleSystem => particleSystem.Stop(true));

            if (_trailRenderers.Count != 0)
            {
                _trailRenderers.ForEach(trailRenderer =>
                {
                    trailRenderer.emitting = false;
                    trailRenderer.Clear();
                });
            }
            
            IsActive = false;
        }

        public void SetColor(Color color)
        {
            if(_particleSystems.Count != 0) 
                _particleSystems.ForEach(particleSystem => particleSystem.startColor = color);
        }
    }   
}