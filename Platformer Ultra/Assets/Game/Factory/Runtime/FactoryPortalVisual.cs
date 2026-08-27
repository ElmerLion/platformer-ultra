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
        [SerializeField] private ParticleSystem[] _particles = System.Array.Empty<ParticleSystem>();
        [SerializeField, Min(0.05f)] private float _activationDuration = 0.8f;
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

            _energyRoot.Rotate(0f, 0f, _activeRotationSpeed * Time.deltaTime, Space.Self);
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
    }
}