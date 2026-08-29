using System;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    public enum ProceduralEnemyRigKind
    {
        Saboteur,
        Armored
    }

    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ProceduralEnemyAnimator : MonoBehaviour, ICinematicAttackPerformer
    {
        [Header("Runtime Sources")]
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private MonoBehaviour _motorBehaviour;
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private EnemyAttackController _attackController;
        [SerializeField] private EnemyHealth _health;

        [Header("Rig")]
        [SerializeField] private ProceduralEnemyRigKind _rigKind;
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
        [SerializeField] private Transform _leftWeapon;
        [SerializeField] private Transform _rightWeapon;
        [SerializeField] private Transform _energyCore;
        [SerializeField] private Transform _backAssembly;

        [Header("Motion Tuning")]
        [SerializeField, Min(0.1f)] private float _strideDistance = 1.1f;
        [SerializeField, Range(5f, 65f)] private float _legSwing = 32f;
        [SerializeField, Min(1f)] private float _response = 11f;

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
        private LocalPose _leftWeaponBind;
        private LocalPose _rightWeaponBind;
        private LocalPose _energyCoreBind;
        private LocalPose _backAssemblyBind;

        private IEnemyMotor _motor;
        private IEnemyTraversalMotor _traversalMotor;
        private float _clock;
        private float _gaitPhase;
        private float _speedWeight;
        private float _chaseWeight;
        private float _attackElapsed;
        private float _attackDuration = 1f;
        private float _impactPulse;
        private float _damagePulse;
        private float _deathElapsed;
        private float _idleOffset;
        private bool _attacking;
        private bool _specialAttack;
        private bool _dead;
        private bool _initialized;
        private bool _trailsEmitting;
        private TrailRenderer[] _weaponTrails;

        public ProceduralEnemyRigKind RigKind => _rigKind;
        public bool RigConfigured =>
            _rigRoot != null && _pelvis != null && _chest != null && _head != null &&
            _leftUpperArm != null && _rightUpperArm != null &&
            _leftThigh != null && _rightThigh != null;

        public event Action Footstepped;

        private void Awake()
        {
            ResolveReferences();
            CaptureBindPose();
            _weaponTrails = GetComponentsInChildren<TrailRenderer>(true);
            SetWeaponTrails(false);
            _idleOffset = Mathf.Abs(GetInstanceID() % 997) * 0.0137f;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetWeaponTrails(false);
        }

        private void LateUpdate()
        {
            if (!_initialized || !RigConfigured)
            {
                return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            _clock += deltaTime;
            if (_attacking)
            {
                _attackElapsed += deltaTime;
            }

            if (_dead)
            {
                _deathElapsed += deltaTime;
            }

            _impactPulse = Mathf.MoveTowards(_impactPulse, 0f, deltaTime * 4.8f);
            _damagePulse = Mathf.MoveTowards(_damagePulse, 0f, deltaTime * 6.5f);

            if (!_dead && !_attacking && _traversalMotor != null && _traversalMotor.IsTraversing)
            {
                SetWeaponTrails(false);
                ApplyTraversalPose(
                    _traversalMotor.ActiveTraversalKind,
                    _traversalMotor.TraversalProgress);
                return;
            }

            float speed = _motor != null
                ? Vector3.ProjectOnPlane(_motor.Velocity, Vector3.up).magnitude
                : 0f;
            bool chasing = _brain != null && _brain.IsTargetingPlayer;
            float referenceSpeed = _definition != null
                ? Mathf.Max(0.1f, chasing ? _definition.PlayerChaseSpeed : _definition.MachineTravelSpeed)
                : 3f;
            float blend = 1f - Mathf.Exp(-_response * deltaTime);
            _speedWeight = Mathf.Lerp(_speedWeight, Mathf.Clamp01(speed / referenceSpeed), blend);
            _chaseWeight = Mathf.Lerp(_chaseWeight, chasing ? 1f : 0f, blend);
            if (!_dead && !_attacking && speed > 0.02f)
            {
                int previousStep = Mathf.FloorToInt(_gaitPhase / Mathf.PI);
                _gaitPhase += speed / Mathf.Max(0.1f, _strideDistance) * Mathf.PI * 2f * deltaTime;
                int currentStep = Mathf.FloorToInt(_gaitPhase / Mathf.PI);
                if (currentStep != previousStep && _speedWeight > 0.4f)
                {
                    Footstepped?.Invoke();
                }
            }

            if (_rigKind == ProceduralEnemyRigKind.Saboteur)
            {
                float attackProgress = _attacking
                    ? Mathf.Clamp01(_attackElapsed / Mathf.Max(0.05f, _attackDuration))
                    : 0f;
                SetWeaponTrails(_attacking && !_dead && attackProgress >= 0.14f && attackProgress <= 0.73f);
                ApplySaboteurPose();
            }
            else
            {
                SetWeaponTrails(false);
                ApplyArmoredPose();
            }
        }

        public void ConfigureRig(
            ProceduralEnemyRigKind rigKind,
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
            Transform leftWeapon,
            Transform rightWeapon,
            Transform energyCore,
            Transform backAssembly,
            float strideDistance,
            float legSwing)
        {
            _rigKind = rigKind;
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
            _leftWeapon = leftWeapon;
            _rightWeapon = rightWeapon;
            _energyCore = energyCore;
            _backAssembly = backAssembly;
            _strideDistance = Mathf.Max(0.1f, strideDistance);
            _legSwing = Mathf.Clamp(legSwing, 5f, 65f);
        }

        public void ConfigureRuntime(
            EnemyDefinition definition,
            MonoBehaviour motorBehaviour,
            EnemyBrain brain,
            EnemyAttackController attackController,
            EnemyHealth health)
        {
            Unsubscribe();
            _definition = definition;
            _motorBehaviour = motorBehaviour;
            _brain = brain;
            _attackController = attackController;
            _health = health;
            ResolveReferences();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void PlayCinematicAttack(float duration)
        {
            if (_dead)
            {
                return;
            }

            _attacking = true;
            _specialAttack = false;
            _attackElapsed = 0f;
            _attackDuration = Mathf.Max(0.05f, duration);
        }

        public void StopCinematicAttack()
        {
            _attacking = false;
            _specialAttack = false;
            _attackElapsed = 0f;
            SetWeaponTrails(false);
        }

        private void ApplyTraversalPose(EnemyTraversalKind kind, float progress)
        {
            float normalized = Mathf.Clamp01(progress);
            bool armored = _rigKind == ProceduralEnemyRigKind.Armored;
            if (kind == EnemyTraversalKind.Jump)
            {
                float arc = Mathf.Sin(normalized * Mathf.PI);
                float tuck = Mathf.Sin(normalized * Mathf.PI) * (armored ? 0.7f : 1f);
                Apply(_rigRoot, _rigRootBind, new Vector3(0f, -0.05f * tuck, 0f),
                    new Vector3(armored ? 9f : 14f, 0f, 0f), Vector3.one);
                Apply(_pelvis, _pelvisBind, Vector3.zero, new Vector3(-12f * tuck, 0f, 0f), Vector3.one);
                Apply(_chest, _chestBind, Vector3.zero, new Vector3(18f * arc, 0f, 0f), Vector3.one);
                Apply(_head, _headBind, Vector3.zero, new Vector3(-10f * arc, 0f, 0f), Vector3.one);
                Apply(_leftUpperArm, _leftUpperArmBind, Vector3.zero,
                    new Vector3(-36f * arc, 0f, -22f * arc), Vector3.one);
                Apply(_rightUpperArm, _rightUpperArmBind, Vector3.zero,
                    new Vector3(-36f * arc, 0f, 22f * arc), Vector3.one);
                Apply(_leftForearm, _leftForearmBind, Vector3.zero,
                    new Vector3(-28f * tuck, 0f, 0f), Vector3.one);
                Apply(_rightForearm, _rightForearmBind, Vector3.zero,
                    new Vector3(-28f * tuck, 0f, 0f), Vector3.one);
                Apply(_leftThigh, _leftThighBind, Vector3.zero,
                    new Vector3(42f * tuck, 0f, -5f), Vector3.one);
                Apply(_rightThigh, _rightThighBind, Vector3.zero,
                    new Vector3(42f * tuck, 0f, 5f), Vector3.one);
                Apply(_leftShin, _leftShinBind, Vector3.zero,
                    new Vector3(62f * tuck, 0f, 0f), Vector3.one);
                Apply(_rightShin, _rightShinBind, Vector3.zero,
                    new Vector3(62f * tuck, 0f, 0f), Vector3.one);
                Apply(_leftFoot, _leftFootBind, Vector3.zero, Vector3.zero, Vector3.one);
                Apply(_rightFoot, _rightFootBind, Vector3.zero, Vector3.zero, Vector3.one);
            }
            else
            {
                float cycle = Mathf.Sin((_clock * (armored ? 5.4f : 7.2f)) + normalized * Mathf.PI * 4f);
                float opposite = -cycle;
                float reach = armored ? 34f : 48f;
                Apply(_rigRoot, _rigRootBind, new Vector3(0f, Mathf.Abs(cycle) * 0.025f, 0f),
                    new Vector3(-8f, 0f, cycle * 2f), Vector3.one);
                Apply(_pelvis, _pelvisBind, Vector3.zero, new Vector3(0f, cycle * 4f, 0f), Vector3.one);
                Apply(_chest, _chestBind, Vector3.zero, new Vector3(-12f, opposite * 5f, 0f), Vector3.one);
                Apply(_head, _headBind, Vector3.zero, new Vector3(8f, cycle * 3f, 0f), Vector3.one);
                Apply(_leftUpperArm, _leftUpperArmBind, Vector3.zero,
                    new Vector3(-62f + cycle * reach, -8f, -18f), Vector3.one);
                Apply(_rightUpperArm, _rightUpperArmBind, Vector3.zero,
                    new Vector3(-62f + opposite * reach, 8f, 18f), Vector3.one);
                Apply(_leftForearm, _leftForearmBind, Vector3.zero,
                    new Vector3(-42f - Mathf.Max(0f, cycle) * 24f, 0f, 0f), Vector3.one);
                Apply(_rightForearm, _rightForearmBind, Vector3.zero,
                    new Vector3(-42f - Mathf.Max(0f, opposite) * 24f, 0f, 0f), Vector3.one);
                Apply(_leftThigh, _leftThighBind, Vector3.zero,
                    new Vector3(opposite * reach * 0.75f, 0f, -3f), Vector3.one);
                Apply(_rightThigh, _rightThighBind, Vector3.zero,
                    new Vector3(cycle * reach * 0.75f, 0f, 3f), Vector3.one);
                Apply(_leftShin, _leftShinBind, Vector3.zero,
                    new Vector3(24f + Mathf.Max(0f, cycle) * 36f, 0f, 0f), Vector3.one);
                Apply(_rightShin, _rightShinBind, Vector3.zero,
                    new Vector3(24f + Mathf.Max(0f, opposite) * 36f, 0f, 0f), Vector3.one);
                Apply(_leftFoot, _leftFootBind, Vector3.zero, new Vector3(-12f * cycle, 0f, 0f), Vector3.one);
                Apply(_rightFoot, _rightFootBind, Vector3.zero, new Vector3(-12f * opposite, 0f, 0f), Vector3.one);
            }

            Apply(_leftWeapon, _leftWeaponBind, Vector3.zero, Vector3.zero, Vector3.one);
            Apply(_rightWeapon, _rightWeaponBind, Vector3.zero, Vector3.zero, Vector3.one);
            Apply(_energyCore, _energyCoreBind, Vector3.zero, Vector3.zero, Vector3.one);
            Apply(_backAssembly, _backAssemblyBind, Vector3.zero, Vector3.zero, Vector3.one);
        }

        private void ApplySaboteurPose()
        {
            float gait = Mathf.Sin(_gaitPhase);
            float oppositeGait = Mathf.Sin(_gaitPhase + Mathf.PI);
            float step = Mathf.Abs(gait) * _speedWeight;
            float idle = Mathf.Sin((_clock + _idleOffset) * 2.2f);
            float attackT = _attacking ? Mathf.Clamp01(_attackElapsed / Mathf.Max(0.05f, _attackDuration)) : 0f;
            float anticipation = Envelope(attackT, 0f, 0.19f, 0.36f);
            float slash = Envelope(attackT, 0.18f, 0.43f, 0.67f);
            float recovery = Envelope(attackT, 0.52f, 0.7f, 1f);
            float deathT = _dead ? Smooth01(Mathf.Clamp01(_deathElapsed / 0.82f)) : 0f;

            Apply(_rigRoot, _rigRootBind,
                new Vector3(0f, step * 0.035f - deathT * 0.28f, 0f),
                new Vector3(10f * _chaseWeight + deathT * 38f, slash * -7f, gait * 3.5f * _speedWeight + deathT * 78f),
                Vector3.one);
            Apply(_pelvis, _pelvisBind,
                new Vector3(0f, step * 0.018f, 0f),
                new Vector3(0f, gait * 9f * _speedWeight + slash * 18f, gait * 2f * _speedWeight),
                Vector3.one);
            Apply(_chest, _chestBind,
                new Vector3(0f, idle * 0.007f, 0f),
                new Vector3(4f * _chaseWeight + anticipation * -9f + slash * 13f,
                    -gait * 11f * _speedWeight - anticipation * 31f + slash * 52f,
                    _damagePulse * 8f),
                Vector3.one);
            Apply(_head, _headBind, Vector3.zero,
                new Vector3(-4f * _chaseWeight, Mathf.Sin((_clock + _idleOffset) * 0.83f) * (1f - _speedWeight) * 11f - slash * 22f,
                    _damagePulse * -6f),
                Vector3.one);

            float armSwing = _legSwing * 0.76f * _speedWeight;
            Apply(_leftUpperArm, _leftUpperArmBind, Vector3.zero,
                new Vector3(oppositeGait * armSwing - anticipation * 74f - slash * 22f + recovery * 18f,
                    anticipation * -18f + slash * 34f,
                    -8f - anticipation * 44f + slash * 78f), Vector3.one);
            Apply(_rightUpperArm, _rightUpperArmBind, Vector3.zero,
                new Vector3(gait * armSwing - anticipation * 82f - slash * 34f + recovery * 22f,
                    anticipation * 23f - slash * 39f,
                    8f + anticipation * 48f - slash * 82f), Vector3.one);
            Apply(_leftForearm, _leftForearmBind, Vector3.zero,
                new Vector3(-Mathf.Max(0f, gait) * 24f - anticipation * 23f + slash * 39f, 0f, slash * -20f), Vector3.one);
            Apply(_rightForearm, _rightForearmBind, Vector3.zero,
                new Vector3(-Mathf.Max(0f, oppositeGait) * 24f - anticipation * 28f + slash * 43f, 0f, slash * 20f), Vector3.one);

            Apply(_leftThigh, _leftThighBind, Vector3.zero,
                new Vector3(gait * _legSwing * _speedWeight + deathT * 18f, 0f, -2f), Vector3.one);
            Apply(_rightThigh, _rightThighBind, Vector3.zero,
                new Vector3(oppositeGait * _legSwing * _speedWeight - deathT * 22f, 0f, 2f), Vector3.one);
            Apply(_leftShin, _leftShinBind, Vector3.zero,
                new Vector3(Mathf.Max(0f, -gait) * 48f * _speedWeight + deathT * 34f, 0f, 0f), Vector3.one);
            Apply(_rightShin, _rightShinBind, Vector3.zero,
                new Vector3(Mathf.Max(0f, -oppositeGait) * 48f * _speedWeight + deathT * 21f, 0f, 0f), Vector3.one);
            Apply(_leftFoot, _leftFootBind, Vector3.zero,
                new Vector3(-gait * 10f * _speedWeight, 0f, 0f), Vector3.one);
            Apply(_rightFoot, _rightFootBind, Vector3.zero,
                new Vector3(-oppositeGait * 10f * _speedWeight, 0f, 0f), Vector3.one);

            Apply(_leftWeapon, _leftWeaponBind, Vector3.zero,
                new Vector3(0f, slash * 18f, idle * 2f), Vector3.one * (1f + _impactPulse * 0.08f));
            Apply(_rightWeapon, _rightWeaponBind, Vector3.zero,
                new Vector3(0f, slash * -18f, -idle * 2f), Vector3.one * (1f + _impactPulse * 0.08f));
            float coreScale = 1f + Mathf.Sin((_clock + _idleOffset) * 6.2f) * 0.06f +
                              _chaseWeight * 0.1f + _impactPulse * 0.22f;
            Apply(_energyCore, _energyCoreBind, Vector3.zero, Vector3.zero, Vector3.one * coreScale);
            Apply(_backAssembly, _backAssemblyBind, Vector3.zero,
                new Vector3(idle * 1.5f, gait * 4f * _speedWeight, -gait * 2f * _speedWeight), Vector3.one);
        }

        private void ApplyArmoredPose()
        {
            float gait = Mathf.Sin(_gaitPhase);
            float oppositeGait = Mathf.Sin(_gaitPhase + Mathf.PI);
            float stomp = Mathf.Pow(Mathf.Abs(Mathf.Sin(_gaitPhase)), 2.2f) * _speedWeight;
            float idle = Mathf.Sin((_clock + _idleOffset) * 1.35f);
            float attackT = _attacking ? Mathf.Clamp01(_attackElapsed / Mathf.Max(0.05f, _attackDuration)) : 0f;
            float anticipation = Envelope(attackT, 0f, _specialAttack ? 0.34f : 0.24f, _specialAttack ? 0.54f : 0.43f);
            float strike = Envelope(attackT, _specialAttack ? 0.38f : 0.24f, _specialAttack ? 0.61f : 0.51f,
                _specialAttack ? 0.79f : 0.72f);
            float recovery = Envelope(attackT, 0.64f, 0.81f, 1f);
            float deathT = _dead ? Smooth01(Mathf.Clamp01(_deathElapsed / 1.3f)) : 0f;
            float crouch = _specialAttack ? anticipation * 0.34f : anticipation * 0.12f;

            Apply(_rigRoot, _rigRootBind,
                new Vector3(0f, stomp * 0.055f - crouch - deathT * 0.62f, 0f),
                new Vector3(_chaseWeight * 3f + strike * (_specialAttack ? 12f : 5f) + deathT * 71f,
                    gait * 1.8f * _speedWeight,
                    gait * 2.2f * _speedWeight + deathT * -16f),
                new Vector3(1f + crouch * 0.035f, 1f - crouch * 0.06f, 1f + crouch * 0.035f));
            Apply(_pelvis, _pelvisBind, Vector3.zero,
                new Vector3(0f, gait * 4.5f * _speedWeight, gait * 1.2f * _speedWeight), Vector3.one);
            Apply(_chest, _chestBind,
                new Vector3(0f, idle * 0.012f, 0f),
                new Vector3(anticipation * -8f + strike * 14f, -gait * 5f * _speedWeight + strike * 9f,
                    _damagePulse * 3.5f), Vector3.one);
            Apply(_head, _headBind, Vector3.zero,
                new Vector3(-_chaseWeight * 5f + anticipation * 7f - strike * 12f,
                    Mathf.Sin((_clock + _idleOffset) * 0.47f) * (1f - _speedWeight) * 6f,
                    _damagePulse * -3f), Vector3.one);

            float armSwing = _legSwing * 0.42f * _speedWeight;
            float specialRaise = _specialAttack ? anticipation * -96f : 0f;
            float specialSlam = _specialAttack ? strike * 116f : 0f;
            Apply(_leftUpperArm, _leftUpperArmBind, Vector3.zero,
                new Vector3(oppositeGait * armSwing + specialRaise + specialSlam + anticipation * (_specialAttack ? 0f : -28f),
                    strike * -8f,
                    -11f - anticipation * 8f + strike * 13f), Vector3.one);
            Apply(_rightUpperArm, _rightUpperArmBind, Vector3.zero,
                new Vector3(gait * armSwing + specialRaise + specialSlam + anticipation * (_specialAttack ? 0f : -87f) +
                    strike * (_specialAttack ? 0f : 124f),
                    strike * 8f,
                    11f + anticipation * 12f - strike * 17f), Vector3.one);
            Apply(_leftForearm, _leftForearmBind, Vector3.zero,
                new Vector3(-18f - Mathf.Max(0f, gait) * 17f * _speedWeight - anticipation * 18f + strike * 29f,
                    0f, 0f), Vector3.one);
            Apply(_rightForearm, _rightForearmBind, Vector3.zero,
                new Vector3(-18f - Mathf.Max(0f, oppositeGait) * 17f * _speedWeight - anticipation * 28f + strike * 37f,
                    0f, 0f), Vector3.one);

            Apply(_leftThigh, _leftThighBind, Vector3.zero,
                new Vector3(gait * _legSwing * _speedWeight + crouch * 32f + deathT * 28f, 0f, -2f), Vector3.one);
            Apply(_rightThigh, _rightThighBind, Vector3.zero,
                new Vector3(oppositeGait * _legSwing * _speedWeight + crouch * 32f - deathT * 12f, 0f, 2f), Vector3.one);
            Apply(_leftShin, _leftShinBind, Vector3.zero,
                new Vector3(Mathf.Max(0f, -gait) * 35f * _speedWeight + crouch * 52f, 0f, 0f), Vector3.one);
            Apply(_rightShin, _rightShinBind, Vector3.zero,
                new Vector3(Mathf.Max(0f, -oppositeGait) * 35f * _speedWeight + crouch * 52f, 0f, 0f), Vector3.one);
            Apply(_leftFoot, _leftFootBind, Vector3.zero,
                new Vector3(-gait * 7f * _speedWeight, 0f, 0f), Vector3.one);
            Apply(_rightFoot, _rightFootBind, Vector3.zero,
                new Vector3(-oppositeGait * 7f * _speedWeight, 0f, 0f), Vector3.one);

            float fistPulse = 1f + _impactPulse * 0.13f;
            Apply(_leftWeapon, _leftWeaponBind, Vector3.zero,
                new Vector3(strike * 10f, 0f, 0f), Vector3.one * fistPulse);
            Apply(_rightWeapon, _rightWeaponBind, Vector3.zero,
                new Vector3(strike * -10f, 0f, 0f), Vector3.one * fistPulse);
            float coreScale = 1f + Mathf.Sin((_clock + _idleOffset) * 3.4f) * 0.045f +
                              _chaseWeight * 0.08f + anticipation * 0.13f + _impactPulse * 0.24f;
            Apply(_energyCore, _energyCoreBind, Vector3.zero, Vector3.zero, Vector3.one * coreScale);
            Apply(_backAssembly, _backAssemblyBind,
                new Vector3(0f, idle * 0.012f, 0f),
                new Vector3(idle * 1.2f, -gait * 2f * _speedWeight, gait * 1.2f * _speedWeight), Vector3.one);
        }

        private void HandleAttackStarted(bool special)
        {
            if (_dead)
            {
                return;
            }

            _attacking = true;
            _specialAttack = special;
            _attackElapsed = 0f;
            _attackDuration = _definition != null
                ? Mathf.Max(0.05f, special ? _definition.SpecialDuration : _definition.AttackDuration)
                : 1f;
        }

        private void HandleAttackImpacted(bool special, Vector3 position)
        {
            _impactPulse = 1f;
        }

        private void HandleAttackCompleted()
        {
            _attacking = false;
            _specialAttack = false;
            _attackElapsed = 0f;
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            _damagePulse = 1f;
        }

        private void HandleDied(DamageInfo damageInfo)
        {
            _attacking = false;
            _specialAttack = false;
            _dead = true;
            _deathElapsed = 0f;
            _impactPulse = 1f;
        }

        private void Subscribe()
        {
            if (_attackController != null)
            {
                _attackController.AttackStarted -= HandleAttackStarted;
                _attackController.AttackImpacted -= HandleAttackImpacted;
                _attackController.AttackCompleted -= HandleAttackCompleted;
                _attackController.AttackStarted += HandleAttackStarted;
                _attackController.AttackImpacted += HandleAttackImpacted;
                _attackController.AttackCompleted += HandleAttackCompleted;
            }

            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
                _health.Died -= HandleDied;
                _health.Damaged += HandleDamaged;
                _health.Died += HandleDied;
            }
        }

        private void Unsubscribe()
        {
            if (_attackController != null)
            {
                _attackController.AttackStarted -= HandleAttackStarted;
                _attackController.AttackImpacted -= HandleAttackImpacted;
                _attackController.AttackCompleted -= HandleAttackCompleted;
            }

            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
                _health.Died -= HandleDied;
            }
        }

        private void ResolveReferences()
        {
            _motor = _motorBehaviour as IEnemyMotor;
            _traversalMotor = _motorBehaviour as IEnemyTraversalMotor;
        }

        private void SetWeaponTrails(bool emitting)
        {
            if (_weaponTrails == null || _trailsEmitting == emitting)
            {
                return;
            }

            _trailsEmitting = emitting;
            for (int index = 0; index < _weaponTrails.Length; index++)
            {
                TrailRenderer trail = _weaponTrails[index];
                if (trail == null)
                {
                    continue;
                }

                trail.Clear();
                trail.emitting = emitting;
            }
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
            _leftWeaponBind = LocalPose.Capture(_leftWeapon);
            _rightWeaponBind = LocalPose.Capture(_rightWeapon);
            _energyCoreBind = LocalPose.Capture(_energyCore);
            _backAssemblyBind = LocalPose.Capture(_backAssembly);
            _initialized = RigConfigured;
        }

        private static float Envelope(float value, float start, float peak, float end)
        {
            if (value <= start || value >= end)
            {
                return 0f;
            }

            return value <= peak
                ? Smooth01(Mathf.InverseLerp(start, peak, value))
                : 1f - Smooth01(Mathf.InverseLerp(peak, end, value));
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
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
