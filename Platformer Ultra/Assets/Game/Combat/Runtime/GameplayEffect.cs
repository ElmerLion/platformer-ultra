using System;
using System.Collections;
using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    public sealed class GameplayEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] _particleSystems = Array.Empty<ParticleSystem>();
        [SerializeField] private Light _burstLight;
        [SerializeField] private bool _looping;
        [SerializeField, Min(0.05f)] private float _lightDuration = 0.12f;
        [SerializeField, Min(0.1f)] private float _effectLifetime = 1.25f;
        [SerializeField, Min(0f)] private float _stopTailDuration = 0.4f;

        private Coroutine _lifetimeRoutine;
        private float _initialLightIntensity;

        public int ParticleLayerCount => _particleSystems != null ? _particleSystems.Length : 0;
        public bool IsLooping => _looping;
        public float EffectLifetime => _effectLifetime;

        private void Awake()
        {
            _initialLightIntensity = _burstLight != null ? _burstLight.intensity : 0f;
        }

        public void Configure(
            ParticleSystem[] particleSystems,
            Light burstLight,
            bool looping,
            float effectLifetime,
            float lightDuration = 0.12f,
            float stopTailDuration = 0.4f)
        {
            _particleSystems = particleSystems ?? Array.Empty<ParticleSystem>();
            _burstLight = burstLight;
            _looping = looping;
            _effectLifetime = Mathf.Max(0.1f, effectLifetime);
            _lightDuration = Mathf.Max(0.05f, lightDuration);
            _stopTailDuration = Mathf.Max(0f, stopTailDuration);
            _initialLightIntensity = _burstLight != null ? _burstLight.intensity : 0f;
        }

        public void Play()
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

            if (_burstLight != null)
            {
                _burstLight.enabled = true;
                _burstLight.intensity = _initialLightIntensity;
            }

            if (!Application.isPlaying || _looping)
            {
                return;
            }

            RestartLifetimeRoutine(PlayLifetime());
        }

        public void Stop()
        {
            for (int index = 0; index < _particleSystems.Length; index++)
            {
                _particleSystems[index]?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (_burstLight != null)
            {
                _burstLight.enabled = false;
            }

            if (Application.isPlaying)
            {
                RestartLifetimeRoutine(DestroyAfter(_stopTailDuration));
            }
        }

        private void RestartLifetimeRoutine(IEnumerator routine)
        {
            if (_lifetimeRoutine != null)
            {
                StopCoroutine(_lifetimeRoutine);
            }

            _lifetimeRoutine = StartCoroutine(routine);
        }

        private IEnumerator PlayLifetime()
        {
            float elapsed = 0f;
            while (elapsed < _effectLifetime)
            {
                elapsed += Time.deltaTime;
                FadeLight(elapsed);
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator DestroyAfter(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Destroy(gameObject);
        }

        private void FadeLight(float elapsed)
        {
            if (_burstLight == null)
            {
                return;
            }

            float normalized = Mathf.Clamp01(elapsed / _lightDuration);
            _burstLight.intensity = Mathf.Lerp(_initialLightIntensity, 0f, normalized);
            if (normalized >= 1f)
            {
                _burstLight.enabled = false;
            }
        }
    }
}
