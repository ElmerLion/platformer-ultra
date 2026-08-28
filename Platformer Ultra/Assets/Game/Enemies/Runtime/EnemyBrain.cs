using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private EnemyAttackController _attackController;
        [SerializeField] private MonoBehaviour _motorBehaviour;
        [SerializeField] private EnemyAnimatorDriver _animatorDriver;
        [SerializeField, Min(0.05f)] private float _pathRefreshInterval = 0.25f;
        [SerializeField, Min(0f)] private float _attackVerticalTolerance = 2.5f;

        private IEnemyMotor _motor;
        private IEnemyTraversalMotor _traversalMotor;
        private MachineTargetRegistry _machineRegistry;
        private Targetable _player;
        private IFactoryTarget _factoryTarget;
        private IFactoryTarget _previousFactoryTarget;
        private float _outsideDisengageTimer;
        private float _nextPathRefreshTime;
        private bool _targetingPlayer;
        private bool _specialConsideredForApproach;
        private bool _runtimeInitialized;
        private bool _registrySubscribed;

        public EnemyState State { get; private set; } = EnemyState.Spawn;
        public bool IsTargetingPlayer => _targetingPlayer;
        public IFactoryTarget CurrentFactoryTarget => _factoryTarget;
        public IFactoryTarget PreviousFactoryTarget => _previousFactoryTarget;
        public FactoryMachineHealth CurrentMachineTarget => _factoryTarget as FactoryMachineHealth;
        public FactoryMachineHealth PreviousMachineTarget => _previousFactoryTarget as FactoryMachineHealth;
        public Targetable CurrentTarget => _targetingPlayer
            ? _player
            : (_factoryTarget != null ? _factoryTarget.Targetable : null);

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (!_runtimeInitialized || State == EnemyState.Dead)
            {
                return;
            }

            SubscribeToRegistry();
            if (_targetingPlayer)
            {
                if (!IsFactoryTargetValid(_previousFactoryTarget))
                {
                    _previousFactoryTarget = null;
                }
            }
            else if (!IsFactoryTargetValid(_factoryTarget))
            {
                _factoryTarget = null;
                AcquireFactoryTarget();
            }
        }

        private void OnDisable()
        {
            _attackController?.CancelAttack();
            _motor?.Stop();
            UnsubscribeFromRegistry();
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time, Random.value);
        }

        public void Configure(
            EnemyDefinition definition,
            EnemyHealth health,
            EnemyAttackController attackController,
            MonoBehaviour motorBehaviour,
            EnemyAnimatorDriver animatorDriver)
        {
            _definition = definition;
            _health = health;
            _attackController = attackController;
            _motorBehaviour = motorBehaviour;
            _animatorDriver = animatorDriver;
            ResolveReferences();
            _motor?.Configure(definition);
        }

        public void InitializeRuntime(
            MachineTargetRegistry machineRegistry,
            Targetable player,
            EnemyRuntimeRegistry enemyRegistry)
        {
            UnsubscribeFromRegistry();
            _machineRegistry = machineRegistry;
            _player = player;
            _runtimeInitialized = true;
            SubscribeToRegistry();

            _health?.InitializeRuntime(enemyRegistry);
            _factoryTarget = null;
            _previousFactoryTarget = null;
            _targetingPlayer = false;
            _outsideDisengageTimer = 0f;
            _specialConsideredForApproach = false;
            _nextPathRefreshTime = 0f;
            State = EnemyState.AcquireMachine;
            AcquireFactoryTarget();
        }

        public void Tick(float deltaTime, float timestamp, float specialRoll)
        {
            if (_definition == null || _health == null || !_health.IsAlive || State == EnemyState.Dead)
            {
                return;
            }

            if (_traversalMotor != null && _traversalMotor.IsTraversing)
            {
                State = _targetingPlayer ? EnemyState.ChasePlayer : EnemyState.MoveToMachine;
                return;
            }

            UpdatePlayerOverride(Mathf.Max(0f, deltaTime));
            if (!_targetingPlayer && !IsFactoryTargetValid(_factoryTarget))
            {
                _factoryTarget = null;
                AcquireFactoryTarget();
            }

            Targetable target = CurrentTarget;
            if (target == null || !target.IsTargetable)
            {
                _attackController?.CancelAttack();
                _motor?.Stop();
                State = EnemyState.AcquireMachine;
                return;
            }

            Vector3 targetPosition = target.TargetPoint.position;
            if (_attackController != null && _attackController.IsAttacking)
            {
                _motor?.Stop();
                _motor?.FaceTarget(targetPosition, deltaTime);
                State = _targetingPlayer ? EnemyState.AttackPlayer : EnemyState.AttackMachine;
                return;
            }

            float attackRange = _targetingPlayer
                ? _definition.PlayerAttackRange
                : _definition.MachineAttackRange;
            Vector3 targetOffset = targetPosition - transform.position;
            float distance = Vector3.ProjectOnPlane(targetOffset, Vector3.up).magnitude;
            bool withinVerticalTolerance = Mathf.Abs(targetOffset.y) <= _attackVerticalTolerance;

            bool inLeapBand = _definition.Archetype == EnemyArchetype.Armored &&
                              withinVerticalTolerance &&
                              distance >= _definition.MinimumLeapDistance &&
                              distance <= _definition.MaximumLeapDistance;
            if (!inLeapBand)
            {
                _specialConsideredForApproach = false;
            }
            else if (!_specialConsideredForApproach &&
                     _attackController != null &&
                     _attackController.CanBeginAttack(true, timestamp))
            {
                _specialConsideredForApproach = true;
                if (ShouldUseSpecial(timestamp, distance, specialRoll, targetPosition))
                {
                    _motor?.Stop();
                    _motor?.FaceTarget(targetPosition, deltaTime);
                    State = _targetingPlayer ? EnemyState.AttackPlayer : EnemyState.AttackMachine;
                    if (_attackController != null && _attackController.TryBeginAttack(target, true, timestamp))
                    {
                        return;
                    }
                }
            }

            if (withinVerticalTolerance && distance <= attackRange)
            {
                _motor?.Stop();
                _motor?.FaceTarget(targetPosition, deltaTime);
                State = _targetingPlayer ? EnemyState.AttackPlayer : EnemyState.AttackMachine;

                _attackController?.TryBeginAttack(target, false, timestamp);
                return;
            }

            State = _targetingPlayer ? EnemyState.ChasePlayer : EnemyState.MoveToMachine;
            if (_motor != null && timestamp >= _nextPathRefreshTime)
            {
                _motor.SetDestination(targetPosition, attackRange * 0.82f, _targetingPlayer);
                _nextPathRefreshTime = timestamp + _pathRefreshInterval;
            }
        }

        public void Die()
        {
            if (State == EnemyState.Dead)
            {
                return;
            }

            State = EnemyState.Dead;
            _attackController?.CancelAttack();
            _motor?.Stop();
            UnsubscribeFromRegistry();
        }

        public bool TryPlace(Vector3 position, float searchRadius)
        {
            return _motor != null && _motor.TryPlace(position, searchRadius);
        }

        public void ForceMachineTargetForTests(FactoryMachineHealth machine)
        {
            ForceFactoryTargetForTests(machine);
        }

        public void ForceFactoryTargetForTests(IFactoryTarget target)
        {
            _factoryTarget = target;
            _targetingPlayer = false;
        }

        private void UpdatePlayerOverride(float deltaTime)
        {
            bool playerAvailable = _player != null && _player.IsTargetable;
            if (!_targetingPlayer)
            {
                if (!playerAvailable)
                {
                    return;
                }

                float aggroDistance = _definition.PlayerAggroDistance;
                if ((_player.TargetPoint.position - transform.position).sqrMagnitude <= aggroDistance * aggroDistance)
                {
                    EngagePlayer();
                }

                return;
            }

            if (!playerAvailable)
            {
                DisengagePlayer();
                return;
            }

            float disengageDistance = _definition.PlayerDisengageDistance;
            bool outside = (_player.TargetPoint.position - transform.position).sqrMagnitude >
                           disengageDistance * disengageDistance;
            if (!outside)
            {
                _outsideDisengageTimer = 0f;
                return;
            }

            _outsideDisengageTimer += deltaTime;
            if (_outsideDisengageTimer >= _definition.PlayerDisengageDelay)
            {
                DisengagePlayer();
            }
        }

        private void EngagePlayer()
        {
            _previousFactoryTarget = _factoryTarget;
            _targetingPlayer = true;
            _outsideDisengageTimer = 0f;
            _specialConsideredForApproach = false;
            _attackController?.CancelAttack();
            State = EnemyState.ChasePlayer;
        }

        private void DisengagePlayer()
        {
            _targetingPlayer = false;
            _outsideDisengageTimer = 0f;
            _specialConsideredForApproach = false;
            _attackController?.CancelAttack();
            if (IsFactoryTargetValid(_previousFactoryTarget) && IsReachable(_previousFactoryTarget))
            {
                _factoryTarget = _previousFactoryTarget;
            }
            else
            {
                _factoryTarget = null;
                AcquireFactoryTarget();
            }

            State = _factoryTarget != null ? EnemyState.MoveToMachine : EnemyState.AcquireMachine;
        }

        private void AcquireFactoryTarget()
        {
            State = EnemyState.AcquireMachine;
            _specialConsideredForApproach = false;
            if (_machineRegistry == null)
            {
                _factoryTarget = null;
                return;
            }

            _factoryTarget = _machineRegistry.FindNearestEligibleTarget(transform.position, IsReachable);
            if (_factoryTarget != null)
            {
                State = EnemyState.MoveToMachine;
            }
        }

        private bool IsReachable(IFactoryTarget target)
        {
            return target != null && target.Targetable != null &&
                   (_motor == null || _motor.CanReach(
                       target.Targetable.TargetPoint.position,
                       Mathf.Max(1f, _definition.MachineAttackRange)));
        }

        private static bool IsFactoryTargetValid(IFactoryTarget target)
        {
            return target != null && target.IsEligibleTarget &&
                   target.Targetable != null && target.Targetable.IsTargetable;
        }

        private bool ShouldUseSpecial(float timestamp, float distance, float specialRoll, Vector3 targetPosition)
        {
            if (_definition.Archetype != EnemyArchetype.Armored || _attackController == null ||
                specialRoll > _definition.SpecialChance)
            {
                return false;
            }

            bool pathReasonable = _motor != null &&
                                  _motor.CanReach(targetPosition, _definition.SpecialImpactRadius) &&
                                  _motor.TryResolveLanding(
                                      targetPosition,
                                      Mathf.Max(1f, _definition.SpecialImpactRadius),
                                      out _);
            return ArmoredSpecialAttackPolicy.IsEligible(
                timestamp,
                _attackController.LastSpecialAttackTime,
                _definition.SpecialCooldown,
                distance,
                _definition.MinimumLeapDistance,
                _definition.MaximumLeapDistance,
                pathReasonable);
        }

        private void HandleMachineRegistryChanged()
        {
            if (!_targetingPlayer && !IsFactoryTargetValid(_factoryTarget))
            {
                _factoryTarget = null;
            }

            if (_targetingPlayer && !IsFactoryTargetValid(_previousFactoryTarget))
            {
                _previousFactoryTarget = null;
            }
        }

        private void ResolveReferences()
        {
            _motor = _motorBehaviour as IEnemyMotor;
            _traversalMotor = _motorBehaviour as IEnemyTraversalMotor;
        }

        private void SubscribeToRegistry()
        {
            if (_registrySubscribed || _machineRegistry == null)
            {
                return;
            }

            _machineRegistry.Changed += HandleMachineRegistryChanged;
            _registrySubscribed = true;
        }

        private void UnsubscribeFromRegistry()
        {
            if (_registrySubscribed && _machineRegistry != null)
            {
                _machineRegistry.Changed -= HandleMachineRegistryChanged;
            }

            _registrySubscribed = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (_definition == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.2f, 0.15f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _definition.PlayerAggroDistance);
            Gizmos.color = new Color(1f, 0.65f, 0.15f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _definition.PlayerDisengageDistance);
        }
    }
}
