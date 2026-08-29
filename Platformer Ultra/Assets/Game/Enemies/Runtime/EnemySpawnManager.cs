using System;
using System.Collections.Generic;
using PlatformerUltra.Combat;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawnManager : MonoBehaviour, IEnemySpawningController
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
        [SerializeField] private FactoryObjectiveTerminal _spawnUnlockTerminal;
        [SerializeField] private FactoryObjectiveTerminal _assemblerTerminal;
        [SerializeField] private CameraShakeController _cameraShake;

        [Header("Pacing")]
        [SerializeField, Min(0f)] private float _initialDelay = 6f;
        [SerializeField, Min(0.1f)] private float _minimumSpawnInterval = 12f;
        [SerializeField, Min(0.1f)] private float _maximumSpawnInterval = 18f;
        [SerializeField, Min(1)] private int _minimumBurstSize = 2;
        [SerializeField, Min(1)] private int _maximumBurstSize = 3;
        [SerializeField, Min(0.05f)] private float _burstSpawnInterval = 0.8f;
        [SerializeField, Min(1)] private int _activeEnemyCap = 6;
        [SerializeField, Min(0f)] private float _armoredEscalationDelay = 60f;

        private float _nextSpawnTime;
        private float _spawningUnlockedAt = float.PositiveInfinity;
        private int _remainingBurstSpawns;
        private bool _spawningUnlocked;
        private bool _spawningEnabled = true;
        private bool _subscribed;

        public int ActiveEnemyCap => _activeEnemyCap;
        public int MinimumBurstSize => _minimumBurstSize;
        public int MaximumBurstSize => _maximumBurstSize;
        public float BurstSpawnInterval => _burstSpawnInterval;
        public float ArmoredEscalationDelay => _armoredEscalationDelay;
        public IReadOnlyList<EnemySpawnPoint> SpawnPoints => _spawnPoints;
        public bool SpawningUnlocked => _spawningUnlocked;
        public bool SpawningEnabled => _spawningEnabled;
        public bool ArmoredUnlocked => (_assemblerTerminal != null && _assemblerTerminal.IsActivated) ||
                                       (SpawningUnlocked && Time.time >= _spawningUnlockedAt + _armoredEscalationDelay);

        public event Action<EnemyHealth, EnemySpawnPoint> EnemySpawned;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            TryUnlockSpawning(Time.time);
            _nextSpawnTime = Time.time + _initialDelay;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            TrySpawn(
                Time.time,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value);
        }

        public void Configure(
            GameObject dronePrefab,
            GameObject saboteurPrefab,
            GameObject armoredPrefab,
            EnemySpawnPoint[] spawnPoints,
            Targetable player,
            MachineTargetRegistry machineRegistry,
            EnemyRuntimeRegistry enemyRegistry,
            FactoryObjectiveTerminal spawnUnlockTerminal,
            FactoryObjectiveTerminal assemblerTerminal,
            float initialDelay,
            float minimumSpawnInterval,
            float maximumSpawnInterval,
            int activeEnemyCap,
            float armoredEscalationDelay,
            CameraShakeController cameraShake = null)
        {
            Configure(
                dronePrefab,
                saboteurPrefab,
                armoredPrefab,
                spawnPoints,
                player,
                machineRegistry,
                enemyRegistry,
                spawnUnlockTerminal,
                assemblerTerminal,
                initialDelay,
                minimumSpawnInterval,
                maximumSpawnInterval,
                activeEnemyCap,
                armoredEscalationDelay,
                2,
                3,
                0.8f,
                cameraShake);
        }

        public void Configure(
            GameObject dronePrefab,
            GameObject saboteurPrefab,
            GameObject armoredPrefab,
            EnemySpawnPoint[] spawnPoints,
            Targetable player,
            MachineTargetRegistry machineRegistry,
            EnemyRuntimeRegistry enemyRegistry,
            FactoryObjectiveTerminal spawnUnlockTerminal,
            FactoryObjectiveTerminal assemblerTerminal,
            float initialDelay,
            float minimumSpawnInterval,
            float maximumSpawnInterval,
            int activeEnemyCap,
            float armoredEscalationDelay,
            int minimumBurstSize,
            int maximumBurstSize,
            float burstSpawnInterval,
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
            _spawnUnlockTerminal = spawnUnlockTerminal;
            _assemblerTerminal = assemblerTerminal;
            _initialDelay = Mathf.Max(0f, initialDelay);
            _minimumSpawnInterval = Mathf.Max(0.1f, minimumSpawnInterval);
            _maximumSpawnInterval = Mathf.Max(_minimumSpawnInterval, maximumSpawnInterval);
            _minimumBurstSize = Mathf.Max(1, minimumBurstSize);
            _maximumBurstSize = Mathf.Max(_minimumBurstSize, maximumBurstSize);
            _burstSpawnInterval = Mathf.Max(0.05f, burstSpawnInterval);
            _activeEnemyCap = Mathf.Max(1, activeEnemyCap);
            _armoredEscalationDelay = Mathf.Max(0f, armoredEscalationDelay);
            _cameraShake = cameraShake;
            _spawningUnlocked = false;
            _spawningUnlockedAt = float.PositiveInfinity;
            _remainingBurstSpawns = 0;
            _spawningEnabled = true;
            Subscribe();
            TryUnlockSpawning(Time.time);
        }

        public bool TrySpawn(float timestamp, float pointSample, float weightSample, float intervalSample)
        {
            return TrySpawn(timestamp, pointSample, weightSample, intervalSample, intervalSample);
        }

        public bool TrySpawn(
            float timestamp,
            float pointSample,
            float weightSample,
            float intervalSample,
            float burstSizeSample)
        {
            if (!_spawningEnabled || timestamp < _nextSpawnTime || !SpawningUnlocked ||
                _machineRegistry == null || !_machineRegistry.HasOperationalMachines ||
                _enemyRegistry == null ||
                !CanSpawnForCap(_enemyRegistry.ActiveCount, _activeEnemyCap) ||
                _spawnPoints == null || _spawnPoints.Length == 0)
            {
                return false;
            }

            if (_remainingBurstSpawns <= 0)
            {
                _remainingBurstSpawns = SampleBurstSize(
                    _minimumBurstSize,
                    _maximumBurstSize,
                    burstSizeSample);
            }

            bool armoredAllowed = (_assemblerTerminal != null && _assemblerTerminal.IsActivated) ||
                                   timestamp >= _spawningUnlockedAt + _armoredEscalationDelay;
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
                _remainingBurstSpawns = Mathf.Max(0, _remainingBurstSpawns - 1);
                _nextSpawnTime = _remainingBurstSpawns > 0
                    ? timestamp + _burstSpawnInterval
                    : timestamp + Mathf.Lerp(
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

        public static int SampleBurstSize(int minimumBurstSize, int maximumBurstSize, float sample)
        {
            int minimum = Mathf.Max(1, minimumBurstSize);
            int maximum = Mathf.Max(minimum, maximumBurstSize);
            int optionCount = maximum - minimum + 1;
            int offset = Mathf.Min(optionCount - 1, Mathf.FloorToInt(Mathf.Clamp01(sample) * optionCount));
            return minimum + offset;
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

        public void SetSpawningEnabled(bool enabled)
        {
            _spawningEnabled = enabled;
        }

        private void HandleSpawnUnlockTerminalActivated(FactoryObjectiveTerminal terminal)
        {
            TryUnlockSpawning(Time.time);
        }

        private void HandleSpawnUnlockMachineStateChanged(
            FactoryObjectiveTerminal terminal,
            FactoryMachineState state)
        {
            TryUnlockSpawning(Time.time);
        }

        private void TryUnlockSpawning(float timestamp)
        {
            if (_spawningUnlocked || _spawnUnlockTerminal == null || !_spawnUnlockTerminal.IsOperational)
            {
                return;
            }

            _spawningUnlocked = true;
            _spawningUnlockedAt = timestamp;
            _nextSpawnTime = Mathf.Max(_nextSpawnTime, timestamp + _initialDelay);
        }

        private void Subscribe()
        {
            if (_subscribed || _spawnUnlockTerminal == null)
            {
                return;
            }

            _spawnUnlockTerminal.Activated += HandleSpawnUnlockTerminalActivated;
            _spawnUnlockTerminal.MachineStateChanged += HandleSpawnUnlockMachineStateChanged;
            _subscribed = true;
            TryUnlockSpawning(Time.time);
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _spawnUnlockTerminal == null)
            {
                return;
            }

            _spawnUnlockTerminal.Activated -= HandleSpawnUnlockTerminalActivated;
            _spawnUnlockTerminal.MachineStateChanged -= HandleSpawnUnlockMachineStateChanged;
            _subscribed = false;
        }
    }
}
