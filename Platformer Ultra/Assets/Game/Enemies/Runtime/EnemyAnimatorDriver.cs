using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimatorDriver : MonoBehaviour
    {
        public const string MoveSpeedParameter = "MoveSpeed";
        public const string LocomotionRateParameter = "LocomotionRate";
        public const string ChasingPlayerParameter = "ChasingPlayer";
        public const string AttackParameter = "Attack";
        public const string SpecialAttackParameter = "SpecialAttack";

        [SerializeField] private Animator _animator;
        [SerializeField] private MonoBehaviour _motorBehaviour;
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private EnemyBrain _brain;
        [SerializeField, Min(0f)] private float _speedDamping = 0.08f;

        private static readonly int MoveSpeedHash = Animator.StringToHash(MoveSpeedParameter);
        private static readonly int LocomotionRateHash = Animator.StringToHash(LocomotionRateParameter);
        private static readonly int ChasingPlayerHash = Animator.StringToHash(ChasingPlayerParameter);
        private static readonly int AttackHash = Animator.StringToHash(AttackParameter);
        private static readonly int SpecialAttackHash = Animator.StringToHash(SpecialAttackParameter);

        private IEnemyMotor _motor;

        private void Awake()
        {
            ResolveReferences();
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        private void Update()
        {
            if (_animator == null || _motor == null || _definition == null)
            {
                return;
            }

            float speed = Vector3.ProjectOnPlane(_motor.Velocity, Vector3.up).magnitude;
            bool chasingPlayer = _brain != null && _brain.IsTargetingPlayer;
            float referenceSpeed = chasingPlayer
                ? Mathf.Max(0.1f, _definition.PlayerChaseSpeed)
                : Mathf.Max(0.1f, _definition.MachineTravelSpeed);
            float playbackRate = speed <= 0.01f ? 1f : Mathf.Clamp(speed / referenceSpeed, 0.35f, 2.5f);

            _animator.SetFloat(MoveSpeedHash, speed, _speedDamping, Time.deltaTime);
            _animator.SetFloat(LocomotionRateHash, playbackRate);
            _animator.SetBool(ChasingPlayerHash, chasingPlayer);
        }

        public void Configure(
            Animator animator,
            MonoBehaviour motorBehaviour,
            EnemyDefinition definition,
            EnemyBrain brain)
        {
            _animator = animator;
            _motorBehaviour = motorBehaviour;
            _definition = definition;
            _brain = brain;
            ResolveReferences();
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        public void PlayAttack(bool special)
        {
            if (_animator == null)
            {
                return;
            }

            int trigger = special ? SpecialAttackHash : AttackHash;
            _animator.ResetTrigger(trigger);
            _animator.SetTrigger(trigger);
        }

        private void ResolveReferences()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            _motor = _motorBehaviour as IEnemyMotor;
        }
    }
}
