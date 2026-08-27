using System.Collections;
using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    public sealed class DeathExplosionEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] _particleSystems = System.Array.Empty<ParticleSystem>();
        [SerializeField] private Light _burstLight;
        [SerializeField, Min(0.05f)] private float _lightDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float _effectLifetime = 1.8f;

        private float _initialLightIntensity;
        private Coroutine _lifetimeRoutine;

        public float EffectLifetime => _effectLifetime;
        public int ParticleLayerCount => _particleSystems != null ? _particleSystems.Length : 0;

        private void Awake()
        {
            if (_burstLight != null)
            {
                _initialLightIntensity = _burstLight.intensity;
            }
        }

        public void Configure(
            ParticleSystem[] particleSystems,
            Light burstLight,
            float lightDuration = 0.2f,
            float effectLifetime = 1.8f)
        {
            _particleSystems = particleSystems ?? System.Array.Empty<ParticleSystem>();
            _burstLight = burstLight;
            _lightDuration = Mathf.Max(0.05f, lightDuration);
            _effectLifetime = Mathf.Max(0.1f, effectLifetime);
            _initialLightIntensity = _burstLight != null ? _burstLight.intensity : 0f;
        }

        public void Play()
        {
            if (_particleSystems != null)
            {
                for (int index = 0; index < _particleSystems.Length; index++)
                {
                    ParticleSystem particleSystem = _particleSystems[index];
                    if (particleSystem == null)
                    {
                        continue;
                    }

                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Play(true);
                }
            }

            if (_burstLight != null)
            {
                _burstLight.enabled = true;
                _burstLight.intensity = _initialLightIntensity;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
            }

            _lifetimeRoutine = StartCoroutine(PlayLifetime());
        }

        private IEnumerator PlayLifetime()
        {
            float elapsed = 0f;
            while (elapsed < _effectLifetime)
            {
                elapsed += Time.deltaTime;
                if (_burstLight != null)
                {
                    float normalized = Mathf.Clamp01(elapsed / _lightDuration);
                    _burstLight.intensity = Mathf.Lerp(_initialLightIntensity, 0f, normalized);
                    if (normalized >= 1f)
                    {
                        _burstLight.enabled = false;
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
