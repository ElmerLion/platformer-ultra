using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [CreateAssetMenu(fileName = "DA_Enemy", menuName = "Platformer Ultra/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity and Assets")]
        [SerializeField] private EnemyArchetype _archetype;
        [SerializeField] private GameObject _visualPrefab;
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private GameObject _spawnPrefab;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField, Min(0f)] private float _spawnWeight = 1f;

        [Header("Survivability and Movement")]
        [SerializeField, Min(1)] private int _maximumHealth = 50;
        [SerializeField, Min(0f)] private float _machineTravelSpeed = 2f;
        [SerializeField, Min(0f)] private float _playerChaseSpeed = 3f;
        [SerializeField, Min(0.01f)] private float _acceleration = 10f;
        [SerializeField, Min(0.01f)] private float _deceleration = 14f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 540f;
        [SerializeField, Min(0f)] private float _hoverHeight = 2.5f;
        [SerializeField, Min(0f)] private float _bobAmplitude = 0.12f;
        [SerializeField, Min(0f)] private float _bobFrequency = 1.4f;

        [Header("Targeting")]
        [SerializeField, Min(0f)] private float _playerAggroDistance = 6f;
        [SerializeField, Min(0f)] private float _playerDisengageDistance = 10f;
        [SerializeField, Min(0f)] private float _playerDisengageDelay = 1f;
        [SerializeField, Min(0f)] private float _machineAttackRange = 2.2f;
        [SerializeField, Min(0f)] private float _playerAttackRange = 2.2f;

        [Header("Regular Attack")]
        [SerializeField, Min(0f)] private float _attackCooldown = 1.2f;
        [SerializeField, Min(0.05f)] private float _attackDuration = 1f;
        [SerializeField, Range(0.05f, 0.95f)] private float _impactNormalizedTime = 0.55f;
        [SerializeField, Min(0)] private int _playerDamage = 10;
        [SerializeField, Min(0)] private int _machineDamage = 10;
        [SerializeField, Min(0f)] private float _telegraphDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float _projectileSpeed = 12f;

        [Header("Armored Special")]
        [SerializeField, Range(0f, 1f)] private float _specialChance;
        [SerializeField, Min(0f)] private float _specialCooldown = 7f;
        [SerializeField, Min(0f)] private float _minimumLeapDistance = 3f;
        [SerializeField, Min(0f)] private float _maximumLeapDistance = 7f;
        [SerializeField, Min(0f)] private float _specialImpactRadius = 2f;
        [SerializeField, Min(0)] private int _specialPlayerDamage = 35;
        [SerializeField, Min(0)] private int _specialMachineDamage = 40;
        [SerializeField, Min(0.05f)] private float _specialDuration = 1.25f;
        [SerializeField, Min(0f)] private float _leapHeight = 1.4f;

        [Header("Death")]
        [SerializeField, Min(0f)] private float _deathRemovalDelay = 1.5f;

        public EnemyArchetype Archetype => _archetype;
        public GameObject VisualPrefab => _visualPrefab;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public GameObject SpawnPrefab => _spawnPrefab;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float SpawnWeight => _spawnWeight;
        public int MaximumHealth => _maximumHealth;
        public float MachineTravelSpeed => _machineTravelSpeed;
        public float PlayerChaseSpeed => _playerChaseSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float RotationSpeed => _rotationSpeed;
        public float HoverHeight => _hoverHeight;
        public float BobAmplitude => _bobAmplitude;
        public float BobFrequency => _bobFrequency;
        public float PlayerAggroDistance => _playerAggroDistance;
        public float PlayerDisengageDistance => Mathf.Max(_playerAggroDistance, _playerDisengageDistance);
        public float PlayerDisengageDelay => _playerDisengageDelay;
        public float MachineAttackRange => _machineAttackRange;
        public float PlayerAttackRange => _playerAttackRange;
        public float AttackCooldown => _attackCooldown;
        public float AttackDuration => _attackDuration;
        public float ImpactNormalizedTime => _impactNormalizedTime;
        public int PlayerDamage => _playerDamage;
        public int MachineDamage => _machineDamage;
        public float TelegraphDuration => _telegraphDuration;
        public float ProjectileSpeed => _projectileSpeed;
        public float SpecialChance => _specialChance;
        public float SpecialCooldown => _specialCooldown;
        public float MinimumLeapDistance => _minimumLeapDistance;
        public float MaximumLeapDistance => Mathf.Max(_minimumLeapDistance, _maximumLeapDistance);
        public float SpecialImpactRadius => _specialImpactRadius;
        public int SpecialPlayerDamage => _specialPlayerDamage;
        public int SpecialMachineDamage => _specialMachineDamage;
        public float SpecialDuration => _specialDuration;
        public float LeapHeight => _leapHeight;
        public float DeathRemovalDelay => _deathRemovalDelay;

        public void ConfigureIdentity(
            EnemyArchetype archetype,
            GameObject visualPrefab,
            RuntimeAnimatorController animatorController,
            GameObject spawnPrefab,
            GameObject projectilePrefab,
            float spawnWeight)
        {
            _archetype = archetype;
            _visualPrefab = visualPrefab;
            _animatorController = animatorController;
            _spawnPrefab = spawnPrefab;
            _projectilePrefab = projectilePrefab;
            _spawnWeight = Mathf.Max(0f, spawnWeight);
        }

        public void ConfigureMovement(
            int maximumHealth,
            float machineTravelSpeed,
            float playerChaseSpeed,
            float acceleration,
            float deceleration,
            float rotationSpeed,
            float hoverHeight,
            float bobAmplitude,
            float bobFrequency)
        {
            _maximumHealth = Mathf.Max(1, maximumHealth);
            _machineTravelSpeed = Mathf.Max(0f, machineTravelSpeed);
            _playerChaseSpeed = Mathf.Max(0f, playerChaseSpeed);
            _acceleration = Mathf.Max(0.01f, acceleration);
            _deceleration = Mathf.Max(0.01f, deceleration);
            _rotationSpeed = Mathf.Max(0f, rotationSpeed);
            _hoverHeight = Mathf.Max(0f, hoverHeight);
            _bobAmplitude = Mathf.Max(0f, bobAmplitude);
            _bobFrequency = Mathf.Max(0f, bobFrequency);
        }

        public void ConfigureTargeting(
            float playerAggroDistance,
            float playerDisengageDistance,
            float playerDisengageDelay,
            float machineAttackRange,
            float playerAttackRange)
        {
            _playerAggroDistance = Mathf.Max(0f, playerAggroDistance);
            _playerDisengageDistance = Mathf.Max(_playerAggroDistance, playerDisengageDistance);
            _playerDisengageDelay = Mathf.Max(0f, playerDisengageDelay);
            _machineAttackRange = Mathf.Max(0f, machineAttackRange);
            _playerAttackRange = Mathf.Max(0f, playerAttackRange);
        }

        public void ConfigureRegularAttack(
            float cooldown,
            float duration,
            float impactNormalizedTime,
            int playerDamage,
            int machineDamage,
            float telegraphDuration,
            float projectileSpeed)
        {
            _attackCooldown = Mathf.Max(0f, cooldown);
            _attackDuration = Mathf.Max(0.05f, duration);
            _impactNormalizedTime = Mathf.Clamp(impactNormalizedTime, 0.05f, 0.95f);
            _playerDamage = Mathf.Max(0, playerDamage);
            _machineDamage = Mathf.Max(0, machineDamage);
            _telegraphDuration = Mathf.Max(0f, telegraphDuration);
            _projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        }

        public void ConfigureSpecial(
            float chance,
            float cooldown,
            float minimumLeapDistance,
            float maximumLeapDistance,
            float impactRadius,
            int playerDamage,
            int machineDamage,
            float duration,
            float leapHeight)
        {
            _specialChance = Mathf.Clamp01(chance);
            _specialCooldown = Mathf.Max(0f, cooldown);
            _minimumLeapDistance = Mathf.Max(0f, minimumLeapDistance);
            _maximumLeapDistance = Mathf.Max(_minimumLeapDistance, maximumLeapDistance);
            _specialImpactRadius = Mathf.Max(0f, impactRadius);
            _specialPlayerDamage = Mathf.Max(0, playerDamage);
            _specialMachineDamage = Mathf.Max(0, machineDamage);
            _specialDuration = Mathf.Max(0.05f, duration);
            _leapHeight = Mathf.Max(0f, leapHeight);
        }

        public void SetSpawnPrefab(GameObject spawnPrefab)
        {
            _spawnPrefab = spawnPrefab;
        }
    }
}
