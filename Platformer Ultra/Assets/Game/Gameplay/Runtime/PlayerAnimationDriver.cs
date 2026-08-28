using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        public const string MoveSpeedParameter = "MoveSpeed";
        public const string LocomotionRateParameter = "LocomotionRate";
        public const string IsSprintingParameter = "IsSprinting";
        public const string IsGroundedParameter = "IsGrounded";
        public const string VerticalSpeedParameter = "VerticalSpeed";

        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private ThirdPersonPlayerController _controller;

        [Header("Foot Lock Tuning")]
        [SerializeField, Min(0.01f)] private float _walkCycleDistance = 1f;
        [SerializeField, Min(0.01f)] private float _walkClipLength = 1.033f;
        [SerializeField, Min(0.01f)] private float _runCycleDistance = 2.585f;
        [SerializeField, Min(0.01f)] private float _runClipLength = 0.733333f;
        [SerializeField, Min(0f)] private float _minimumLocomotionRate = 0.25f;
        [SerializeField, Min(0.01f)] private float _maximumLocomotionRate = 4f;
        [SerializeField, Min(0f)] private float _speedDamping = 0.08f;

        private static readonly int MoveSpeedHash = Animator.StringToHash(MoveSpeedParameter);
        private static readonly int LocomotionRateHash = Animator.StringToHash(LocomotionRateParameter);
        private static readonly int IsSprintingHash = Animator.StringToHash(IsSprintingParameter);
        private static readonly int IsGroundedHash = Animator.StringToHash(IsGroundedParameter);
        private static readonly int VerticalSpeedHash = Animator.StringToHash(VerticalSpeedParameter);

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        private void Update()
        {
            if (_animator == null || _controller == null)
            {
                return;
            }

            float planarSpeed = _controller.AnimationPlanarSpeed;
            bool isSprinting = _controller.IsSprinting && planarSpeed > 0.05f;
            float locomotionRate = CalculateLocomotionPlaybackRate(
                planarSpeed,
                isSprinting ? _runCycleDistance : _walkCycleDistance,
                isSprinting ? _runClipLength : _walkClipLength,
                _minimumLocomotionRate,
                _maximumLocomotionRate);

            _animator.SetFloat(MoveSpeedHash, planarSpeed, _speedDamping, Time.deltaTime);
            _animator.SetFloat(LocomotionRateHash, locomotionRate);
            _animator.SetBool(IsSprintingHash, isSprinting);
            _animator.SetBool(IsGroundedHash, _controller.IsAnimationGrounded);
            _animator.SetFloat(VerticalSpeedHash, _controller.Velocity.y);
        }

        public void Configure(
            Animator animator,
            ThirdPersonPlayerController controller,
            float walkCycleDistance,
            float walkClipLength,
            float runCycleDistance,
            float runClipLength)
        {
            _animator = animator;
            _controller = controller;
            _walkCycleDistance = Mathf.Max(0.01f, walkCycleDistance);
            _walkClipLength = Mathf.Max(0.01f, walkClipLength);
            _runCycleDistance = Mathf.Max(0.01f, runCycleDistance);
            _runClipLength = Mathf.Max(0.01f, runClipLength);
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        public static float CalculateLocomotionPlaybackRate(
            float planarSpeed,
            float walkCycleDistance,
            float walkClipLength,
            float minimumRate,
            float maximumRate)
        {
            if (planarSpeed <= 0.01f)
            {
                return 1f;
            }

            float safeDistance = Mathf.Max(0.01f, walkCycleDistance);
            float safeLength = Mathf.Max(0.01f, walkClipLength);
            float rawRate = planarSpeed * safeLength / safeDistance;
            return Mathf.Clamp(rawRate, minimumRate, Mathf.Max(minimumRate, maximumRate));
        }
    }
}
