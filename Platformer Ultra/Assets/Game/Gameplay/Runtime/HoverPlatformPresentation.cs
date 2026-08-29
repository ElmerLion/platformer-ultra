using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HoverPlatformPresentation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private BoxCollider _landingTrigger;
        [SerializeField] private Transform _repulsorRing;
        [SerializeField] private Light _hoverLight;
        [SerializeField] private ParticleSystem _hoverParticles;

        [Header("Idle Hover")]
        [SerializeField, Min(0f)] private float _idleAmplitude = 0.018f;
        [SerializeField, Min(0f)] private float _idleFrequency = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _phaseOffset;
        [SerializeField] private float _ringRotationSpeed = 24f;
        [SerializeField, Range(0f, 1f)] private float _lightPulseAmount = 0.12f;

        [Header("Landing Recoil")]
        [SerializeField, Min(0f)] private float _minimumImpactSpeed = 2.25f;
        [SerializeField, Min(0f)] private float _fullImpactSpeed = 12f;
        [SerializeField, Min(0f)] private float _maximumLandingDip = 0.055f;
        [SerializeField, Min(0f)] private float _maximumTiltDegrees = 1.25f;
        [SerializeField, Min(0.05f)] private float _settleDuration = 0.5f;

        private Vector3 _visualRestPosition;
        private Quaternion _visualRestRotation = Quaternion.identity;
        private float _baseLightIntensity;
        private float _landingElapsed = float.PositiveInfinity;
        private float _landingStrength;
        private Vector2 _landingDirection = Vector2.up;
        private ThirdPersonPlayerController _trackedPlayer;
        private int _trackedColliderCount;

        public Transform VisualRoot => _visualRoot;
        public BoxCollider LandingTrigger => _landingTrigger;
        public Transform RepulsorRing => _repulsorRing;
        public Light HoverLight => _hoverLight;
        public ParticleSystem HoverParticles => _hoverParticles;
        public float IdleAmplitude => _idleAmplitude;
        public float IdleFrequency => _idleFrequency;
        public float MaximumLandingDip => _maximumLandingDip;
        public float MaximumTiltDegrees => _maximumTiltDegrees;
        public float SettleDuration => _settleDuration;
        public float LandingStrength => _landingStrength;
        public bool IsTrackingPlayer => _trackedPlayer != null;

        private void Awake()
        {
            CacheRestPose();
            EnsureLandingTrigger();
        }

        private void OnEnable()
        {
            CacheRestPose();
            EnsureLandingTrigger();
        }

        private void Update()
        {
            UpdatePresentation(Time.time, Time.deltaTime);
        }

        private void OnDisable()
        {
            ReleaseTrackedPlayer();
            ResetPresentation();
        }

        private void OnValidate()
        {
            _idleAmplitude = Mathf.Max(0f, _idleAmplitude);
            _idleFrequency = Mathf.Max(0f, _idleFrequency);
            _minimumImpactSpeed = Mathf.Max(0f, _minimumImpactSpeed);
            _fullImpactSpeed = Mathf.Max(_minimumImpactSpeed + 0.01f, _fullImpactSpeed);
            _maximumLandingDip = Mathf.Max(0f, _maximumLandingDip);
            _maximumTiltDegrees = Mathf.Max(0f, _maximumTiltDegrees);
            _settleDuration = Mathf.Max(0.05f, _settleDuration);
            EnsureLandingTrigger();

            if (!Application.isPlaying)
            {
                CacheRestPose();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ThirdPersonPlayerController player = other != null
                ? other.GetComponentInParent<ThirdPersonPlayerController>()
                : null;
            if (player == null)
            {
                return;
            }

            if (_trackedPlayer == player)
            {
                _trackedColliderCount++;
                return;
            }

            ReleaseTrackedPlayer();
            _trackedPlayer = player;
            _trackedColliderCount = 1;
            _trackedPlayer.Landed += HandlePlayerLanded;
        }

        private void OnTriggerExit(Collider other)
        {
            ThirdPersonPlayerController player = other != null
                ? other.GetComponentInParent<ThirdPersonPlayerController>()
                : null;
            if (player == null || player != _trackedPlayer)
            {
                return;
            }

            _trackedColliderCount = Mathf.Max(0, _trackedColliderCount - 1);
            if (_trackedColliderCount == 0)
            {
                ReleaseTrackedPlayer();
            }
        }

        public void Configure(
            Transform visualRoot,
            BoxCollider landingTrigger,
            Transform repulsorRing,
            Light hoverLight,
            ParticleSystem hoverParticles,
            float phaseOffset,
            float idleAmplitude = 0.018f,
            float idleFrequency = 0.55f,
            float minimumImpactSpeed = 2.25f,
            float fullImpactSpeed = 12f,
            float maximumLandingDip = 0.055f,
            float maximumTiltDegrees = 1.25f,
            float settleDuration = 0.5f)
        {
            _visualRoot = visualRoot;
            _landingTrigger = landingTrigger;
            _repulsorRing = repulsorRing;
            _hoverLight = hoverLight;
            _hoverParticles = hoverParticles;
            _phaseOffset = Mathf.Repeat(phaseOffset, 1f);
            _idleAmplitude = Mathf.Max(0f, idleAmplitude);
            _idleFrequency = Mathf.Max(0f, idleFrequency);
            _minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
            _fullImpactSpeed = Mathf.Max(_minimumImpactSpeed + 0.01f, fullImpactSpeed);
            _maximumLandingDip = Mathf.Max(0f, maximumLandingDip);
            _maximumTiltDegrees = Mathf.Max(0f, maximumTiltDegrees);
            _settleDuration = Mathf.Max(0.05f, settleDuration);
            CacheRestPose();
            EnsureLandingTrigger();
            ResetPresentation();
        }

        public void ReactToLanding(float impactSpeed, Vector3 landingWorldPosition)
        {
            if (_visualRoot == null || impactSpeed < _minimumImpactSpeed)
            {
                return;
            }

            _landingStrength = Mathf.InverseLerp(_minimumImpactSpeed, _fullImpactSpeed, impactSpeed);
            _landingElapsed = 0f;

            Vector3 localLandingPosition = transform.InverseTransformPoint(landingWorldPosition);
            Vector3 localVisualCenter = transform.InverseTransformPoint(_visualRoot.position);
            Vector2 direction = new Vector2(
                localLandingPosition.x - localVisualCenter.x,
                localLandingPosition.z - localVisualCenter.z);
            _landingDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.up;
        }

        public void ResetPresentation()
        {
            _landingElapsed = float.PositiveInfinity;
            _landingStrength = 0f;
            if (_visualRoot != null)
            {
                _visualRoot.localPosition = _visualRestPosition;
                _visualRoot.localRotation = _visualRestRotation;
            }

            if (_hoverLight != null)
            {
                _hoverLight.intensity = _baseLightIntensity;
            }
        }

        private void HandlePlayerLanded(float impactSpeed)
        {
            if (_trackedPlayer != null)
            {
                ReactToLanding(impactSpeed, _trackedPlayer.transform.position);
            }
        }

        private void UpdatePresentation(float scaledTime, float deltaTime)
        {
            if (_visualRoot == null)
            {
                return;
            }

            float phase = (scaledTime * _idleFrequency + _phaseOffset) * Mathf.PI * 2f;
            float idleOffset = Mathf.Sin(phase) * _idleAmplitude;
            float recoilOffset = 0f;
            float recoilTilt = 0f;
            float recoilEnvelope = 0f;

            if (_landingElapsed < _settleDuration)
            {
                _landingElapsed = Mathf.Min(_settleDuration, _landingElapsed + Mathf.Max(0f, deltaTime));
                float normalized = Mathf.Clamp01(_landingElapsed / _settleDuration);
                recoilEnvelope = 1f - Mathf.SmoothStep(0f, 1f, normalized);
                float oscillation = Mathf.Sin(normalized * Mathf.PI * 2f);
                recoilOffset = -oscillation * recoilEnvelope * _maximumLandingDip * _landingStrength;
                recoilTilt = oscillation * recoilEnvelope * _maximumTiltDegrees * _landingStrength;

                if (_landingElapsed >= _settleDuration)
                {
                    _landingStrength = 0f;
                }
            }

            _visualRoot.localPosition = _visualRestPosition + Vector3.up * (idleOffset + recoilOffset);
            Quaternion recoilRotation = Quaternion.Euler(
                _landingDirection.y * recoilTilt,
                0f,
                -_landingDirection.x * recoilTilt);
            _visualRoot.localRotation = _visualRestRotation * recoilRotation;

            if (_repulsorRing != null)
            {
                _repulsorRing.Rotate(0f, _ringRotationSpeed * Mathf.Max(0f, deltaTime), 0f, Space.Self);
            }

            if (_hoverLight != null)
            {
                float pulse = 1f + Mathf.Sin(phase) * _lightPulseAmount;
                float landingBoost = recoilEnvelope * _landingStrength * 0.18f;
                _hoverLight.intensity = _baseLightIntensity * (pulse + landingBoost);
            }
        }

        private void CacheRestPose()
        {
            if (_visualRoot != null)
            {
                _visualRestPosition = _visualRoot.localPosition;
                _visualRestRotation = _visualRoot.localRotation;
            }

            if (_hoverLight != null)
            {
                _baseLightIntensity = _hoverLight.intensity;
            }
        }

        private void EnsureLandingTrigger()
        {
            if (_landingTrigger != null)
            {
                _landingTrigger.isTrigger = true;
            }
        }

        private void ReleaseTrackedPlayer()
        {
            if (_trackedPlayer != null)
            {
                _trackedPlayer.Landed -= HandlePlayerLanded;
            }

            _trackedPlayer = null;
            _trackedColliderCount = 0;
        }
    }
}
