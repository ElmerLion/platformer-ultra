using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ProceduralPlayerAnimator : MonoBehaviour
    {
        [Header("Runtime Source")]
        [SerializeField] private ThirdPersonPlayerController _controller;

        [Header("Procedural Rig")]
        [SerializeField] private Transform _rigRoot;
        [SerializeField] private Transform _pelvis;
        [SerializeField] private Transform _chest;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftUpperArm;
        [SerializeField] private Transform _rightUpperArm;
        [SerializeField] private Transform _leftForearm;
        [SerializeField] private Transform _rightForearm;
        [SerializeField] private Transform _leftThigh;
        [SerializeField] private Transform _rightThigh;
        [SerializeField] private Transform _leftShin;
        [SerializeField] private Transform _rightShin;
        [SerializeField] private Transform _leftFoot;
        [SerializeField] private Transform _rightFoot;
        [SerializeField] private Transform _backpack;
        [SerializeField] private Transform _leftDashFin;
        [SerializeField] private Transform _rightDashFin;
        [SerializeField] private Transform _energyCore;

        [Header("Motion Tuning")]
        [SerializeField, Min(0.1f)] private float _walkStrideDistance = 1.05f;
        [SerializeField, Min(0.1f)] private float _runStrideDistance = 1.65f;
        [SerializeField, Range(5f, 80f)] private float _walkLegSwing = 29f;
        [SerializeField, Range(5f, 80f)] private float _runLegSwing = 42f;
        [SerializeField, Min(1f)] private float _response = 14f;

        private LocalPose _rigRootBind;
        private LocalPose _pelvisBind;
        private LocalPose _chestBind;
        private LocalPose _headBind;
        private LocalPose _leftUpperArmBind;
        private LocalPose _rightUpperArmBind;
        private LocalPose _leftForearmBind;
        private LocalPose _rightForearmBind;
        private LocalPose _leftThighBind;
        private LocalPose _rightThighBind;
        private LocalPose _leftShinBind;
        private LocalPose _rightShinBind;
        private LocalPose _leftFootBind;
        private LocalPose _rightFootBind;
        private LocalPose _backpackBind;
        private LocalPose _leftDashFinBind;
        private LocalPose _rightDashFinBind;
        private LocalPose _energyCoreBind;

        private float _clock;
        private float _gaitPhase;
        private float _moveWeight;
        private float _sprintWeight;
        private float _airWeight;
        private float _dashWeight;
        private float _landingPulse;
        private float _jumpPulse;
        private float _doubleJumpPulse;
        private bool _wasGrounded;
        private bool _initialized;

        public bool RigConfigured =>
            _rigRoot != null && _pelvis != null && _chest != null && _head != null &&
            _leftUpperArm != null && _rightUpperArm != null &&
            _leftThigh != null && _rightThigh != null;

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponentInParent<ThirdPersonPlayerController>();
            }

            CaptureBindPose();
            _wasGrounded = _controller == null || _controller.IsAnimationGrounded;
        }

        private void OnEnable()
        {
            SubscribeToController();
        }

        private void OnDisable()
        {
            UnsubscribeFromController();
        }

        private void LateUpdate()
        {
            if (!_initialized || !RigConfigured)
            {
                return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            _clock += deltaTime;

            float planarSpeed = _controller != null ? _controller.AnimationPlanarSpeed : 0f;
            bool grounded = _controller == null || _controller.IsAnimationGrounded;
            bool sprinting = _controller != null && _controller.IsSprinting && planarSpeed > 0.05f;
            bool dashing = _controller != null && _controller.IsDashing;
            float verticalSpeed = _controller != null ? _controller.Velocity.y : 0f;

            if (grounded && !_wasGrounded)
            {
                _landingPulse = Mathf.Clamp01(Mathf.Abs(verticalSpeed) * 0.08f + 0.55f);
            }

            _wasGrounded = grounded;
            float blend = 1f - Mathf.Exp(-_response * deltaTime);
            _moveWeight = Mathf.Lerp(_moveWeight, planarSpeed > 0.08f && grounded ? 1f : 0f, blend);
            _sprintWeight = Mathf.Lerp(_sprintWeight, sprinting ? 1f : 0f, blend);
            _airWeight = Mathf.Lerp(_airWeight, grounded ? 0f : 1f, blend);
            _dashWeight = Mathf.Lerp(_dashWeight, dashing ? 1f : 0f, 1f - Mathf.Exp(-22f * deltaTime));
            _landingPulse = Mathf.MoveTowards(_landingPulse, 0f, deltaTime * 3.8f);
            _jumpPulse = Mathf.MoveTowards(_jumpPulse, 0f, deltaTime * 3.1f);
            _doubleJumpPulse = Mathf.MoveTowards(_doubleJumpPulse, 0f, deltaTime * 2.2f);

            float strideDistance = Mathf.Lerp(_walkStrideDistance, _runStrideDistance, _sprintWeight);
            if (grounded && planarSpeed > 0.02f)
            {
                _gaitPhase += planarSpeed / Mathf.Max(0.1f, strideDistance) * Mathf.PI * 2f * deltaTime;
            }

            ApplyPose(planarSpeed, verticalSpeed);
        }

        public void ConfigureRig(
            Transform rigRoot,
            Transform pelvis,
            Transform chest,
            Transform head,
            Transform leftUpperArm,
            Transform rightUpperArm,
            Transform leftForearm,
            Transform rightForearm,
            Transform leftThigh,
            Transform rightThigh,
            Transform leftShin,
            Transform rightShin,
            Transform leftFoot,
            Transform rightFoot,
            Transform backpack,
            Transform leftDashFin,
            Transform rightDashFin,
            Transform energyCore)
        {
            _rigRoot = rigRoot;
            _pelvis = pelvis;
            _chest = chest;
            _head = head;
            _leftUpperArm = leftUpperArm;
            _rightUpperArm = rightUpperArm;
            _leftForearm = leftForearm;
            _rightForearm = rightForearm;
            _leftThigh = leftThigh;
            _rightThigh = rightThigh;
            _leftShin = leftShin;
            _rightShin = rightShin;
            _leftFoot = leftFoot;
            _rightFoot = rightFoot;
            _backpack = backpack;
            _leftDashFin = leftDashFin;
            _rightDashFin = rightDashFin;
            _energyCore = energyCore;
        }

        public void BindController(ThirdPersonPlayerController controller)
        {
            if (_controller == controller)
            {
                return;
            }

            UnsubscribeFromController();
            _controller = controller;
            if (isActiveAndEnabled)
            {
                SubscribeToController();
            }
        }

        private void ApplyPose(float planarSpeed, float verticalSpeed)
        {
            float gait = Mathf.Sin(_gaitPhase);
            float oppositeGait = Mathf.Sin(_gaitPhase + Mathf.PI);
            float strideSwing = Mathf.Lerp(_walkLegSwing, _runLegSwing, _sprintWeight) * _moveWeight;
            float idleBreath = Mathf.Sin(_clock * 2.05f);
            float stepLift = Mathf.Abs(Mathf.Sin(_gaitPhase)) * _moveWeight;
            float landingEase = _landingPulse * _landingPulse;
            float dashLean = 20f * _dashWeight;
            float airDirection = Mathf.Clamp(verticalSpeed / 8f, -1f, 1f);

            Apply(
                _rigRoot,
                _rigRootBind,
                new Vector3(0f, stepLift * 0.025f - landingEase * 0.095f, 0f),
                new Vector3(dashLean - _jumpPulse * 7f, 0f, gait * 1.8f * _moveWeight),
                Vector3.one);
            Apply(
                _pelvis,
                _pelvisBind,
                new Vector3(0f, idleBreath * 0.006f + stepLift * 0.018f, 0f),
                new Vector3(landingEase * 9f, gait * 6f * _moveWeight, gait * 2.5f * _moveWeight),
                new Vector3(1f + landingEase * 0.035f, 1f - landingEase * 0.08f, 1f + landingEase * 0.035f));
            Apply(
                _chest,
                _chestBind,
                new Vector3(0f, idleBreath * 0.008f, 0f),
                new Vector3(-dashLean * 0.18f - airDirection * 3f, -gait * 7f * _moveWeight, -gait * 1.7f * _moveWeight),
                Vector3.one);
            Apply(
                _head,
                _headBind,
                Vector3.zero,
                new Vector3(-airDirection * 4f, Mathf.Sin(_clock * 0.72f) * (1f - _moveWeight) * 7f + gait * 2f * _moveWeight, 0f),
                Vector3.one);

            float armSwing = strideSwing * 0.72f;
            float airborneArms = _airWeight * (-16f - airDirection * 7f);
            float dashTrail = _dashWeight * 48f;
            Apply(_leftUpperArm, _leftUpperArmBind, Vector3.zero,
                new Vector3(oppositeGait * armSwing + airborneArms + dashTrail, 0f, -3f), Vector3.one);
            Apply(_rightUpperArm, _rightUpperArmBind, Vector3.zero,
                new Vector3(gait * armSwing + airborneArms + dashTrail, 0f, 3f), Vector3.one);
            Apply(_leftForearm, _leftForearmBind, Vector3.zero,
                new Vector3(-Mathf.Max(0f, gait) * 25f - _dashWeight * 18f, 0f, 0f), Vector3.one);
            Apply(_rightForearm, _rightForearmBind, Vector3.zero,
                new Vector3(-Mathf.Max(0f, oppositeGait) * 25f - _dashWeight * 18f, 0f, 0f), Vector3.one);

            float airTuck = _airWeight * (10f - airDirection * 8f);
            Apply(_leftThigh, _leftThighBind, Vector3.zero,
                new Vector3(gait * strideSwing + airTuck, 0f, -1.5f), Vector3.one);
            Apply(_rightThigh, _rightThighBind, Vector3.zero,
                new Vector3(oppositeGait * strideSwing - airTuck * 0.55f, 0f, 1.5f), Vector3.one);
            Apply(_leftShin, _leftShinBind, Vector3.zero,
                new Vector3(Mathf.Max(0f, -gait) * 42f + _airWeight * 24f, 0f, 0f), Vector3.one);
            Apply(_rightShin, _rightShinBind, Vector3.zero,
                new Vector3(Mathf.Max(0f, -oppositeGait) * 42f + _airWeight * 18f, 0f, 0f), Vector3.one);
            Apply(_leftFoot, _leftFootBind, Vector3.zero,
                new Vector3(-gait * strideSwing * 0.32f - _airWeight * 8f, 0f, 0f), Vector3.one);
            Apply(_rightFoot, _rightFootBind, Vector3.zero,
                new Vector3(-oppositeGait * strideSwing * 0.32f - _airWeight * 8f, 0f, 0f), Vector3.one);

            Apply(_backpack, _backpackBind, new Vector3(0f, idleBreath * 0.004f, 0f),
                new Vector3(-dashLean * 0.2f, gait * 2f * _moveWeight, 0f), Vector3.one);
            Apply(_leftDashFin, _leftDashFinBind, Vector3.zero,
                new Vector3(0f, -42f * _dashWeight, 24f * _dashWeight), Vector3.one);
            Apply(_rightDashFin, _rightDashFinBind, Vector3.zero,
                new Vector3(0f, 42f * _dashWeight, -24f * _dashWeight), Vector3.one);

            float corePulse = 1f + Mathf.Sin(_clock * 4.7f) * 0.035f + _dashWeight * 0.13f + _doubleJumpPulse * 0.2f;
            Apply(_energyCore, _energyCoreBind, Vector3.zero, Vector3.zero, Vector3.one * corePulse);
        }

        private void HandleJumped(bool doubleJump)
        {
            _jumpPulse = 1f;
            if (doubleJump)
            {
                _doubleJumpPulse = 1f;
            }
        }

        private void HandleDashed(Vector3 direction, bool airborne)
        {
            _dashWeight = Mathf.Max(_dashWeight, 0.72f);
            if (airborne)
            {
                _jumpPulse = Mathf.Max(_jumpPulse, 0.4f);
            }
        }

        private void SubscribeToController()
        {
            if (_controller == null)
            {
                return;
            }

            _controller.Jumped -= HandleJumped;
            _controller.Dashed -= HandleDashed;
            _controller.Jumped += HandleJumped;
            _controller.Dashed += HandleDashed;
        }

        private void UnsubscribeFromController()
        {
            if (_controller == null)
            {
                return;
            }

            _controller.Jumped -= HandleJumped;
            _controller.Dashed -= HandleDashed;
        }

        private void CaptureBindPose()
        {
            _rigRootBind = LocalPose.Capture(_rigRoot);
            _pelvisBind = LocalPose.Capture(_pelvis);
            _chestBind = LocalPose.Capture(_chest);
            _headBind = LocalPose.Capture(_head);
            _leftUpperArmBind = LocalPose.Capture(_leftUpperArm);
            _rightUpperArmBind = LocalPose.Capture(_rightUpperArm);
            _leftForearmBind = LocalPose.Capture(_leftForearm);
            _rightForearmBind = LocalPose.Capture(_rightForearm);
            _leftThighBind = LocalPose.Capture(_leftThigh);
            _rightThighBind = LocalPose.Capture(_rightThigh);
            _leftShinBind = LocalPose.Capture(_leftShin);
            _rightShinBind = LocalPose.Capture(_rightShin);
            _leftFootBind = LocalPose.Capture(_leftFoot);
            _rightFootBind = LocalPose.Capture(_rightFoot);
            _backpackBind = LocalPose.Capture(_backpack);
            _leftDashFinBind = LocalPose.Capture(_leftDashFin);
            _rightDashFinBind = LocalPose.Capture(_rightDashFin);
            _energyCoreBind = LocalPose.Capture(_energyCore);
            _initialized = RigConfigured;
        }

        private static void Apply(
            Transform target,
            LocalPose bind,
            Vector3 positionOffset,
            Vector3 eulerOffset,
            Vector3 scaleMultiplier)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = bind.Position + positionOffset;
            target.localRotation = bind.Rotation * Quaternion.Euler(eulerOffset);
            target.localScale = Vector3.Scale(bind.Scale, scaleMultiplier);
        }

        private readonly struct LocalPose
        {
            public LocalPose(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }

            public static LocalPose Capture(Transform target)
            {
                return target != null
                    ? new LocalPose(target.localPosition, target.localRotation, target.localScale)
                    : new LocalPose(Vector3.zero, Quaternion.identity, Vector3.one);
            }
        }
    }
}
