using System;
using System.Collections.Generic;
using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackController : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private EnemyAnimatorDriver _animatorDriver;
        [SerializeField] private MonoBehaviour _motorBehaviour;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private GameObject _telegraphVisual;
        [SerializeField] private LayerMask _impactMask = ~0;
        [SerializeField, Min(0f)] private float _meleeImpactRangeTolerance = 0.35f;
        [SerializeField, Min(0f)] private float _meleeImpactVerticalTolerance = 2.5f;

        private readonly HashSet<IDamageable> _specialVictims = new HashSet<IDamageable>();
        private IEnemyMotor _motor;
        private Targetable _capturedTarget;
        private IDamageable _capturedDamageable;
        private Faction _capturedFaction;
        private Vector3 _leapOrigin;
        private Vector3 _leapLanding;
        private float _attackStartedAt;
        private float _impactAt;
        private float _attackEndsAt;
        private float _nextRegularAttackTime;
        private float _lastSpecialAttackTime = float.NegativeInfinity;
        private bool _specialAttack;
        private bool _rangedAttack;
        private bool _impactApplied;
        private bool _leapActive;

        public bool IsAttacking { get; private set; }
        public bool ImpactApplied => _impactApplied;
        public bool IsSpecialAttack => IsAttacking && _specialAttack;
        public float LastSpecialAttackTime => _lastSpecialAttackTime;

        public event Action<bool> AttackStarted;
        public event Action AttackCompleted;

        private void Awake()
        {
            ResolveReferences();
            SetTelegraphVisible(false);
        }

        private void OnDisable()
        {
            CancelAttack();
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Configure(
            EnemyDefinition definition,
            EnemyAnimatorDriver animatorDriver,
            MonoBehaviour motorBehaviour,
            Transform muzzle,
            GameObject telegraphVisual,
            LayerMask impactMask)
        {
            _definition = definition;
            _animatorDriver = animatorDriver;
            _motorBehaviour = motorBehaviour;
            _muzzle = muzzle;
            _telegraphVisual = telegraphVisual;
            _impactMask = impactMask;
            ResolveReferences();
            SetTelegraphVisible(false);
        }

        public bool CanBeginAttack(bool special, float timestamp)
        {
            if (IsAttacking || _definition == null)
            {
                return false;
            }

            return special
                ? timestamp >= _lastSpecialAttackTime + _definition.SpecialCooldown
                : timestamp >= _nextRegularAttackTime;
        }

        public bool TryBeginAttack(Targetable target, bool special, float timestamp)
        {
            if (target == null || !target.IsTargetable || target.Damageable == null || !CanBeginAttack(special, timestamp))
            {
                return false;
            }

            _capturedTarget = target;
            _capturedDamageable = target.Damageable;
            _capturedFaction = target.Faction;
            _specialAttack = special;
            _rangedAttack = _definition.Archetype == EnemyArchetype.Drone;
            _impactApplied = false;
            _leapActive = false;

            if (special && !BeginLeap())
            {
                ClearCapturedAttack();
                return false;
            }

            _attackStartedAt = timestamp;
            float duration = special ? _definition.SpecialDuration : _definition.AttackDuration;
            _impactAt = timestamp + (_rangedAttack
                ? _definition.TelegraphDuration
                : duration * _definition.ImpactNormalizedTime);
            _attackEndsAt = timestamp + Mathf.Max(duration, _definition.TelegraphDuration + 0.15f);
            _nextRegularAttackTime = timestamp + _definition.AttackCooldown;
            if (special)
            {
                _lastSpecialAttackTime = timestamp;
            }

            IsAttacking = true;
            _animatorDriver?.PlayAttack(special);
            SetTelegraphVisible(_rangedAttack);
            AttackStarted?.Invoke(special);
            return true;
        }

        public void Tick(float deltaTime, float timestamp)
        {
            if (!IsAttacking)
            {
                return;
            }

            if (_leapActive)
            {
                UpdateLeap(timestamp);
            }

            if (!_impactApplied && timestamp >= _impactAt)
            {
                if (_rangedAttack)
                {
                    FireProjectileOrApplyDelayedDamage();
                }
                else if (_animatorDriver == null)
                {
                    OnAttackImpact();
                }
            }

            if (_telegraphVisual != null && _telegraphVisual.activeSelf)
            {
                float duration = Mathf.Max(0.01f, _definition.TelegraphDuration);
                float progress = Mathf.Clamp01((timestamp - _attackStartedAt) / duration);
                float pulse = 0.75f + Mathf.Sin(progress * Mathf.PI * 5f) * 0.15f;
                _telegraphVisual.transform.localScale = Vector3.one * pulse;
            }

            if (timestamp >= _attackEndsAt)
            {
                // Animation Events remain the authoritative impact timing. This fallback
                // prevents a valid attack from silently dealing no damage if an imported
                // clip or controller loses its event during an asset rebuild.
                if (!_impactApplied)
                {
                    if (_specialAttack)
                    {
                        OnSpecialImpact();
                    }
                    else if (_rangedAttack)
                    {
                        FireProjectileOrApplyDelayedDamage();
                    }
                    else
                    {
                        ApplyCapturedDamageOnce(false);
                    }
                }

                CompleteAttack();
            }
        }

        public void OnAttackImpact()
        {
            if (!IsAttacking || _impactApplied)
            {
                return;
            }

            if (_specialAttack)
            {
                OnSpecialImpact();
                return;
            }

            ApplyCapturedDamageOnce(false);
        }

        public void OnSpecialImpact()
        {
            if (!IsAttacking || !_specialAttack || _impactApplied)
            {
                return;
            }

            _impactApplied = true;
            if (_leapActive)
            {
                _motor?.SetScriptedPosition(_leapLanding);
                _motor?.EndScriptedMotion(_leapLanding);
                _leapActive = false;
            }

            ApplySpecialAreaDamage();
        }

        public void CancelAttack()
        {
            if (_leapActive)
            {
                Vector3 landingPosition = _leapOrigin;
                if (_motor != null && _motor.TryResolveLanding(
                        transform.position,
                        Mathf.Max(1.5f, _definition != null ? _definition.LeapHeight + 0.75f : 1.5f),
                        out Vector3 resolvedLanding))
                {
                    landingPosition = resolvedLanding;
                }

                _motor?.EndScriptedMotion(landingPosition);
            }

            _leapActive = false;
            IsAttacking = false;
            _capturedTarget = null;
            _capturedDamageable = null;
            SetTelegraphVisible(false);
        }

        private bool BeginLeap()
        {
            if (_motor == null || _capturedTarget == null)
            {
                return false;
            }

            Vector3 targetPosition = _capturedTarget.TargetPoint.position;
            Vector3 planarDirection = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up).normalized;
            Vector3 desiredLanding = targetPosition - planarDirection * 0.9f;
            if (!_motor.TryResolveLanding(desiredLanding, 1.5f, out _leapLanding))
            {
                return false;
            }

            _leapOrigin = transform.position;
            _leapActive = true;
            _motor.BeginScriptedMotion();
            return true;
        }

        private void UpdateLeap(float timestamp)
        {
            float landingDuration = Mathf.Max(0.05f, _definition.SpecialDuration * _definition.ImpactNormalizedTime);
            float progress = Mathf.Clamp01((timestamp - _attackStartedAt) / landingDuration);
            Vector3 position = Vector3.Lerp(_leapOrigin, _leapLanding, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * _definition.LeapHeight;
            _motor.SetScriptedPosition(position);
        }

        private void FireProjectileOrApplyDelayedDamage()
        {
            if (_impactApplied)
            {
                return;
            }

            _impactApplied = true;
            SetTelegraphVisible(false);
            if (_capturedTarget == null || !_capturedTarget.IsTargetable || _capturedDamageable == null || !_capturedDamageable.IsAlive)
            {
                return;
            }

            int damage = GetDamage(_capturedFaction, false);
            if (_definition.ProjectilePrefab == null)
            {
                _capturedDamageable.TakeDamage(new DamageInfo(damage, gameObject, Faction.Enemy, _capturedTarget.TargetPoint.position));
                return;
            }

            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + transform.forward;
            GameObject projectileObject = Instantiate(_definition.ProjectilePrefab, origin, transform.rotation);
            EnemyProjectile projectile = projectileObject.GetComponent<EnemyProjectile>();
            if (projectile == null)
            {
                Destroy(projectileObject);
                _capturedDamageable.TakeDamage(new DamageInfo(damage, gameObject, Faction.Enemy, _capturedTarget.TargetPoint.position));
                return;
            }

            projectile.Initialize(gameObject, _capturedTarget, damage, _definition.ProjectileSpeed, 5f);
        }

        private void ApplyCapturedDamageOnce(bool special)
        {
            if (_impactApplied)
            {
                return;
            }

            _impactApplied = true;
            if (_capturedTarget == null || !_capturedTarget.IsTargetable || _capturedDamageable == null || !_capturedDamageable.IsAlive)
            {
                return;
            }

            if (!special && !_rangedAttack && !IsCapturedTargetWithinMeleeRange())
            {
                return;
            }

            int damage = GetDamage(_capturedFaction, special);
            _capturedDamageable.TakeDamage(new DamageInfo(
                damage,
                gameObject,
                Faction.Enemy,
                _capturedTarget.TargetPoint.position));
        }

        private void ApplySpecialAreaDamage()
        {
            _specialVictims.Clear();
            Collider[] hits = Physics.OverlapSphere(
                _leapLanding,
                _definition.SpecialImpactRadius,
                _impactMask,
                QueryTriggerInteraction.Collide);
            for (int index = 0; index < hits.Length; index++)
            {
                Targetable targetable = hits[index].GetComponentInParent<Targetable>();
                TryApplySpecialDamage(targetable);
            }

            if (IsCapturedTargetWithinSpecialRadius())
            {
                TryApplySpecialDamage(_capturedTarget);
            }
        }

        private bool IsCapturedTargetWithinMeleeRange()
        {
            if (_capturedTarget == null || _definition == null)
            {
                return false;
            }

            Vector3 offset = _capturedTarget.TargetPoint.position - transform.position;
            float planarDistance = Vector3.ProjectOnPlane(offset, Vector3.up).magnitude;
            float attackRange = _capturedFaction == Faction.Player
                ? _definition.PlayerAttackRange
                : _definition.MachineAttackRange;
            return planarDistance <= attackRange + _meleeImpactRangeTolerance &&
                   Mathf.Abs(offset.y) <= _meleeImpactVerticalTolerance;
        }

        private bool IsCapturedTargetWithinSpecialRadius()
        {
            if (_capturedTarget == null || _definition == null)
            {
                return false;
            }

            float impactRadius = _definition.SpecialImpactRadius;
            Vector3 offset = _capturedTarget.TargetPoint.position - _leapLanding;
            return offset.sqrMagnitude <= impactRadius * impactRadius;
        }

        private void TryApplySpecialDamage(Targetable targetable)
        {
            if (targetable == null || !targetable.IsTargetable ||
                (targetable.Faction != Faction.Player && targetable.Faction != Faction.Factory))
            {
                return;
            }

            IDamageable damageable = targetable.Damageable;
            if (damageable == null || !damageable.IsAlive || !_specialVictims.Add(damageable))
            {
                return;
            }

            damageable.TakeDamage(new DamageInfo(
                GetDamage(targetable.Faction, true),
                gameObject,
                Faction.Enemy,
                targetable.TargetPoint.position));
        }

        private int GetDamage(Faction targetFaction, bool special)
        {
            if (targetFaction == Faction.Player)
            {
                return special ? _definition.SpecialPlayerDamage : _definition.PlayerDamage;
            }

            return special ? _definition.SpecialMachineDamage : _definition.MachineDamage;
        }

        private void CompleteAttack()
        {
            if (_leapActive)
            {
                _motor?.EndScriptedMotion(_leapLanding);
                _leapActive = false;
            }

            IsAttacking = false;
            _capturedTarget = null;
            _capturedDamageable = null;
            SetTelegraphVisible(false);
            AttackCompleted?.Invoke();
        }

        private void ClearCapturedAttack()
        {
            _capturedTarget = null;
            _capturedDamageable = null;
            _specialAttack = false;
            _rangedAttack = false;
            _impactApplied = false;
            _leapActive = false;
            SetTelegraphVisible(false);
        }

        private void ResolveReferences()
        {
            _motor = _motorBehaviour as IEnemyMotor;
        }

        private void SetTelegraphVisible(bool visible)
        {
            if (_telegraphVisual != null)
            {
                _telegraphVisual.SetActive(visible);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_definition == null || _definition.Archetype != EnemyArchetype.Armored)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.3f, 0.05f, 0.35f);
            Gizmos.DrawWireSphere(_leapLanding, _definition.SpecialImpactRadius);
        }
    }
}
