using UnityEngine;

namespace PlatformerUltra.Factory
{
    public enum FactoryPortalState
    {
        Inactive,
        Activating,
        Active
    }

    [DisallowMultipleComponent]
    public sealed class FactoryPortalVisual : MonoBehaviour
    {
        [SerializeField] private Transform _energyRoot;
        [SerializeField] private Transform[] _counterRotatingRoots = System.Array.Empty<Transform>();
        [SerializeField] private ParticleSystem[] _particles = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _activationEffects = System.Array.Empty<ParticleSystem>();
        [SerializeField, Min(0.05f)] private float _activationDuration = 1.2f;
        [SerializeField] private float _activeRotationSpeed = 38f;
        [SerializeField] private FactoryPortalState _initialState = FactoryPortalState.Inactive;

        private FactoryPortalState _state;
        private float _activationElapsed;

        public FactoryPortalState State => _state;

        public void Configure(Transform energyRoot, ParticleSystem[] particles)
        {
            _energyRoot = energyRoot;
            _particles = particles ?? System.Array.Empty<ParticleSystem>();
        }

        public void Configure(
            Transform energyRoot,
            Transform[] counterRotatingRoots,
            ParticleSystem[] activeEffects,
            ParticleSystem[] activationEffects)
        {
            _counterRotatingRoots = counterRotatingRoots ?? System.Array.Empty<Transform>();
            _activationEffects = activationEffects ?? System.Array.Empty<ParticleSystem>();
            Configure(energyRoot, activeEffects);
        }

        public void SetState(FactoryPortalState state)
        {
            _state = state;
            _activationElapsed = 0f;

            if (_energyRoot != null)
            {
                _energyRoot.localScale = state == FactoryPortalState.Inactive
                    ? Vector3.zero
                    : state == FactoryPortalState.Activating
                        ? Vector3.one * 0.05f
                        : Vector3.one;
            }

            SetParticlesPlaying(state != FactoryPortalState.Inactive);
            if (state == FactoryPortalState.Activating)
            {
                PlayActivationEffects();
            }
            else if (state == FactoryPortalState.Inactive)
            {
                StopParticles(_activationEffects);
            }
        }

        private void OnEnable()
        {
            SetState(_initialState);
        }

        private void Update()
        {
            if (_energyRoot == null || _state == FactoryPortalState.Inactive)
            {
                return;
            }

            float spinMultiplier = _state == FactoryPortalState.Activating
                ? Mathf.SmoothStep(0.08f, 1f, Mathf.Clamp01(_activationElapsed / _activationDuration))
                : 1f;
            _energyRoot.Rotate(0f, 0f, _activeRotationSpeed * 0.35f * spinMultiplier * Time.deltaTime, Space.Self);
            for (int index = 0; index < _counterRotatingRoots.Length; index++)
            {
                Transform ring = _counterRotatingRoots[index];
                if (ring == null)
                {
                    continue;
                }

                float direction = index % 2 == 0 ? 1f : -1f;
                ring.Rotate(0f, 0f, _activeRotationSpeed * (0.72f + index * 0.22f) * direction * spinMultiplier * Time.deltaTime, Space.Self);
            }
            if (_state != FactoryPortalState.Activating)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 3.5f) * 0.025f;
                _energyRoot.localScale = Vector3.one * pulse;
                return;
            }

            _activationElapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(_activationElapsed / _activationDuration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            _energyRoot.localScale = Vector3.one * Mathf.Lerp(0.05f, 1f, eased);
            if (normalized >= 1f)
            {
                _state = FactoryPortalState.Active;
            }
        }

        private void SetParticlesPlaying(bool shouldPlay)
        {
            foreach (ParticleSystem particleSystem in _particles)
            {
                if (particleSystem == null)
                {
                    continue;
                }

                if (shouldPlay)
                {
                    particleSystem.Play(true);
                }
                else
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private void PlayActivationEffects()
        {
            foreach (ParticleSystem effect in _activationEffects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.Play(true);
            }
        }

        private static void StopParticles(ParticleSystem[] effects)
        {
            foreach (ParticleSystem effect in effects)
            {
                effect?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
