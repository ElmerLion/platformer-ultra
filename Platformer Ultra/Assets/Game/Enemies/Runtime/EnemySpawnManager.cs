using System;
using System.Collections.Generic;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawnManager : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private GameObject _dronePrefab;
        [SerializeField] private GameObject _saboteurPrefab;
        [SerializeField] private GameObject _armoredPrefab;

        [Header("Scene References")]
        [SerializeField] private EnemySpawnPoint[] _spawnPoints = Array.Empty<EnemySpawnPoint>();
        [SerializeField] private Targetable _player;
        [SerializeField] private MachineTargetRegistry _machineRegistry;
        [SerializeField] private EnemyRuntimeRegistry _enemyRegistry;
        [SerializeField] private FactoryObjectiveTerminal _generatorTerminal;
        [SerializeField] private FactoryObjectiveTerminal _assemblerTerminal;
        [SerializeField] private CameraShakeController _cameraShake;

        [Header("Pacing")]
        [SerializeField, Min(0f)] private float _initialDelay = 6f;
        [SerializeField, Min(0.1f)] private float _minimumSpawnInterval = 12f;
        [SerializeField, Min(0.1f)] private float _maximumSpawnInterval = 18f;
        [SerializeField, Min(1)] private int _activeEnemyCap = 6;
        [SerializeField, Min(0f)] private float _armoredEscalationDelay = 90f;

        private float _nextSpawnTime;
        private float _generatorActivatedAt = float.PositiveInfinity;
        private bool _subscribed;

        public int ActiveEnemyCap => _activeEnemyCap;
        public IReadOnlyList<EnemySpawnPoint> SpawnPoints => _spawnPoints;
        public bool SpawningUnlocked => _generatorTerminal != null && _generatorTerminal.IsActivated;
        public bool ArmoredUnlocked => (_assemblerTerminal != null && _assemblerTerminal.IsActivated) ||
                                       (SpawningUnlocked && Time.time >= _generatorActivatedAt + _armoredEscalationDelay);

        public event Action<EnemyHealth, EnemySpawnPoint> EnemySpawned;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            if (SpawningUnlocked && float.IsPositiveInfinity(_generatorActivatedAt))
            {
                _generatorActivatedAt = Time.time;
            }

            _nextSpawnTime = Time.time + _initialDelay;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            TrySpawn(Time.time, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        public void Configure(
            GameObject dronePrefab,
            GameObject saboteurPrefab,
            GameObject armoredPrefab,
            EnemySpawnPoint[] spawnPoints,
            Targetable player,
            MachineTargetRegistry machineRegistry,
            EnemyRuntimeRegistry enemyRegistry,
            FactoryObjectiveTerminal generatorTerminal,
            FactoryObjectiveTerminal assemblerTerminal,
            float initialDelay,
            float minimumSpawnInterval,
            float maximumSpawnInterval,
            int activeEnemyCap,
            float armoredEscalationDelay,
            CameraShakeController cameraShake = null)
        {
            Unsubscribe();
            _dronePrefab = dronePrefab;
            _saboteurPrefab = saboteurPrefab;
            _armoredPrefab = armoredPrefab;
            _spawnPoints = spawnPoints ?? Array.Empty<EnemySpawnPoint>();
            _player = player;
            _machineRegistry = machineRegistry;
            _enemyRegistry = enemyRegistry;
            _generatorTerminal = generatorTerminal;
            _assemblerTerminal = assemblerTerminal;
            _initialDelay = Mathf.Max(0f, initialDelay);
            _minimumSpawnInterval = Mathf.Max(0.1f, minimumSpawnInterval);
            _maximumSpawnInterval = Mathf.Max(_minimumSpawnInterval, maximumSpawnInterval);
            _activeEnemyCap = Mathf.Max(1, activeEnemyCap);
            _armoredEscalationDelay = Mathf.Max(0f, armoredEscalationDelay);
            _cameraShake = cameraShake;
            Subscribe();
        }

        public bool TrySpawn(float timestamp, float pointSample, float weightSample, float intervalSample)
        {
            if (timestamp < _nextSpawnTime || !SpawningUnlocked ||
                _machineRegistry == null || !_machineRegistry.HasOperationalMachines ||
                _enemyRegistry == null ||
                !CanSpawnForCap(_enemyRegistry.ActiveCount, _activeEnemyCap) ||
                _spawnPoints == null || _spawnPoints.Length == 0)
            {
                return false;
            }

            bool armoredAllowed = (_assemblerTerminal != null && _assemblerTerminal.IsActivated) ||
                                   timestamp >= _generatorActivatedAt + _armoredEscalationDelay;
            int startIndex = Mathf.Min(_spawnPoints.Length - 1, Mathf.FloorToInt(Mathf.Clamp01(pointSample) * _spawnPoints.Length));
            for (int offset = 0; offset < _spawnPoints.Length; offset++)
            {
                EnemySpawnPoint spawnPoint = _spawnPoints[(startIndex + offset) % _spawnPoints.Length];
                if (spawnPoint == null || !spawnPoint.CanSpawn(_player) ||
                    !spawnPoint.TryChooseArchetype(armoredAllowed, weightSample, out EnemyArchetype archetype))
                {
                    continue;
                }

                GameObject prefab = GetPrefab(archetype);
                if (prefab == null)
                {
                    continue;
                }

                Vector3 initialPosition = spawnPoint.SpawnPosition;
                if (archetype == EnemyArchetype.Drone)
                {
                    initialPosition += Vector3.up * 2.5f;
                }

                GameObject instance = Instantiate(prefab, initialPosition, spawnPoint.SpawnRotation);
                EnemyBrain brain = instance.GetComponent<EnemyBrain>();
                EnemyHealth health = instance.GetComponent<EnemyHealth>();
                if (brain == null || health == null || health.Definition == null)
                {
                    Destroy(instance);
                    continue;
                }

                Vector3 placementPosition = archetype == EnemyArchetype.Drone
                    ? spawnPoint.SpawnPosition + Vector3.up * health.Definition.HoverHeight
                    : spawnPoint.SpawnPosition;
                if (!brain.TryPlace(placementPosition, 2f))
                {
                    Destroy(instance);
                    continue;
                }

                brain.InitializeRuntime(_machineRegistry, _player, _enemyRegistry);
                instance.GetComponent<EnemyAttackPresentation>()?.InitializeRuntime(_cameraShake);
                _nextSpawnTime = timestamp + Mathf.Lerp(
                    _minimumSpawnInterval,
                    _maximumSpawnInterval,
                    Mathf.Clamp01(intervalSample));
                EnemySpawned?.Invoke(health, spawnPoint);
                return true;
            }

            _nextSpawnTime = timestamp + 1f;
            return false;
        }

        public static bool CanSpawnForCap(int activeEnemies, int activeEnemyCap)
        {
            return activeEnemyCap > 0 && activeEnemies < activeEnemyCap;
        }

        private GameObject GetPrefab(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Drone:
                    return _dronePrefab;
                case EnemyArchetype.Saboteur:
                    return _saboteurPrefab;
                case EnemyArchetype.Armored:
                    return _armoredPrefab;
                default:
                    return null;
            }
        }

        private void HandleGeneratorActivated(FactoryObjectiveTerminal terminal)
        {
            _generatorActivatedAt = Time.time;
            _nextSpawnTime = Mathf.Max(_nextSpawnTime, Time.time + _initialDelay);
        }

        private void Subscribe()
        {
            if (_subscribed || _generatorTerminal == null)
            {
                return;
            }

            _generatorTerminal.Activated += HandleGeneratorActivated;
            _subscribed = true;
            if (_generatorTerminal.IsActivated && float.IsPositiveInfinity(_generatorActivatedAt))
            {
                _generatorActivatedAt = Time.time;
            }
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _generatorTerminal == null)
            {
                return;
            }

            _generatorTerminal.Activated -= HandleGeneratorActivated;
            _subscribed = false;
        }
    }
}
