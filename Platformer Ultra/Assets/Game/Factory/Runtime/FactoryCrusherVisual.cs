using UnityEngine;

namespace PlatformerUltra.Factory
{
    [DisallowMultipleComponent]
    public sealed class FactoryCrusherVisual : MonoBehaviour
    {
        [SerializeField] private Transform _crushingPlate;
        [SerializeField] private Transform _hydraulicRam;
        [SerializeField] private Transform _recoilRoot;
        [SerializeField] private ParticleSystem[] _impactEffects = System.Array.Empty<ParticleSystem>();
        [SerializeField] private float _topHeight = 3.15f;
        [SerializeField] private float _bottomHeight = 0.85f;
        [SerializeField, Min(0.1f)] private float _cycleDuration = 1.4f;
        [SerializeField] private bool _animate = true;

        private Vector3 _ramPosition;
        private Vector3 _ramScale;
        private Vector3 _recoilPosition;
        private float _previousPhase;

        public void Configure(Transform crushingPlate, float topHeight, float bottomHeight, float cycleDuration)
        {
            _crushingPlate = crushingPlate;
            _topHeight = topHeight;
            _bottomHeight = bottomHeight;
            _cycleDuration = Mathf.Max(0.1f, cycleDuration);
            CacheRestPose();
        }

        public void Configure(
            Transform crushingPlate,
            Transform hydraulicRam,
            Transform recoilRoot,
            ParticleSystem[] impactEffects,
            float topHeight,
            float bottomHeight,
            float cycleDuration)
        {
            _hydraulicRam = hydraulicRam;
            _recoilRoot = recoilRoot;
            _impactEffects = impactEffects ?? System.Array.Empty<ParticleSystem>();
            Configure(crushingPlate, topHeight, bottomHeight, cycleDuration);
        }

        public void SetAnimating(bool animate)
        {
            _animate = animate;
        }

        private void Update()
        {
            if (!_animate || _crushingPlate == null)
            {
                return;
            }

            float phase = Mathf.Repeat(Time.time / _cycleDuration, 1f);
            float descent = phase < 0.36f
                ? Mathf.Pow(Mathf.Clamp01(phase / 0.36f), 2.35f)
                : phase < 0.54f
                    ? 1f
                    : Mathf.SmoothStep(1f, 0f, (phase - 0.54f) / 0.46f);

            Vector3 position = _crushingPlate.localPosition;
            position.y = Mathf.Lerp(_topHeight, _bottomHeight, descent);
            _crushingPlate.localPosition = position;

            float extension = Mathf.Max(0f, _topHeight - position.y);
            if (_hydraulicRam != null)
            {
                _hydraulicRam.localScale = new Vector3(
                    _ramScale.x,
                    _ramScale.y + extension * 0.66f,
                    _ramScale.z);
                _hydraulicRam.localPosition = _ramPosition + Vector3.down * extension * 0.5f;
            }

            if (_recoilRoot != null)
            {
                float impactAge = Mathf.Repeat(phase - 0.36f, 1f);
                float recoil = impactAge < 0.12f
                    ? Mathf.Sin(impactAge / 0.12f * Mathf.PI) * 0.075f
                    : 0f;
                _recoilRoot.localPosition = _recoilPosition + Vector3.up * recoil;
            }

            if (_previousPhase < 0.36f && phase >= 0.36f)
            {
                PlayImpactEffects();
            }

            _previousPhase = phase;
        }

        private void Awake()
        {
            CacheRestPose();
        }

        private void OnValidate()
        {
            _cycleDuration = Mathf.Max(0.1f, _cycleDuration);
            _impactEffects ??= System.Array.Empty<ParticleSystem>();
            if (!Application.isPlaying)
            {
                CacheRestPose();
            }
        }

        private void CacheRestPose()
        {
            if (_hydraulicRam != null)
            {
                _ramPosition = _hydraulicRam.localPosition;
                _ramScale = _hydraulicRam.localScale;
            }

            if (_recoilRoot != null)
            {
                _recoilPosition = _recoilRoot.localPosition;
            }
        }

        private void PlayImpactEffects()
        {
            foreach (ParticleSystem effect in _impactEffects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.Play(true);
            }
        }
    }
}
