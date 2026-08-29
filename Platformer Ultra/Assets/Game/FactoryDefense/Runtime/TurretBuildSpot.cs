using PlatformerUltra.Enemies;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.FactoryDefense
{
    [DisallowMultipleComponent]
    public sealed class TurretBuildSpot : MonoBehaviour, IMaintenanceTimedInteractable, IInteractionFeedback
    {
        [SerializeField] private FactoryTurret _turretPrefab;
        [SerializeField] private Transform _turretMount;
        [SerializeField] private GameObject _damagedInstallation;
        [SerializeField] private Collider _interactionTrigger;
        [SerializeField] private EnemyRuntimeRegistry _enemyRegistry;
        [SerializeField] private MachineTargetRegistry _factoryRegistry;
        [SerializeField, Range(3f, 15f)] private float _buildDuration = 6f;

        private FactoryTurret _builtTurret;
        private string _lastInteractionFeedback = string.Empty;

        public string InteractionPrompt => "Hold [E] to Rebuild Turret";
        public string InteractionActionLabel => "Rebuilding Factory Turret";
        public float InteractionDuration => _buildDuration;
        public string LastInteractionFeedback => _lastInteractionFeedback;
        public FactoryTurret BuiltTurret => _builtTurret;
        public bool IsBuilt => _builtTurret != null && _builtTurret.IsAlive;
        public GameObject DamagedInstallation => _damagedInstallation;
        public Vector3 MaintenanceEffectPosition =>
            (_turretMount != null ? _turretMount : transform).TransformPoint(Vector3.up * 1.15f);

        private void Awake()
        {
            ApplyState();
        }

        private void OnValidate()
        {
            _buildDuration = Mathf.Clamp(_buildDuration, 3f, 15f);
            if (!Application.isPlaying)
            {
                ApplyState();
            }
        }

        public void Configure(
            FactoryTurret turretPrefab,
            Transform turretMount,
            GameObject damagedInstallation,
            Collider interactionTrigger,
            EnemyRuntimeRegistry enemyRegistry,
            MachineTargetRegistry factoryRegistry,
            float buildDuration = 6f)
        {
            _turretPrefab = turretPrefab;
            _turretMount = turretMount != null ? turretMount : transform;
            _damagedInstallation = damagedInstallation;
            _interactionTrigger = interactionTrigger;
            _enemyRegistry = enemyRegistry;
            _factoryRegistry = factoryRegistry;
            _buildDuration = Mathf.Clamp(buildDuration, 3f, 15f);
            ApplyState();
        }

        public void AssignRegistries(
            EnemyRuntimeRegistry enemyRegistry,
            MachineTargetRegistry factoryRegistry)
        {
            _enemyRegistry = enemyRegistry;
            _factoryRegistry = factoryRegistry;
        }

        public bool CanInteract(GameObject interactor)
        {
            return interactor != null && !IsBuilt && _turretPrefab != null &&
                   _enemyRegistry != null && _factoryRegistry != null;
        }

        public void Interact(GameObject interactor)
        {
            _lastInteractionFeedback = IsBuilt
                ? "Turret is operational."
                : "Hold [E] to rebuild the turret.";
        }

        public bool BeginTimedInteraction(GameObject interactor)
        {
            _lastInteractionFeedback = string.Empty;
            return CanInteract(interactor);
        }

        public void CancelTimedInteraction(GameObject interactor)
        {
            _lastInteractionFeedback = "Turret rebuild cancelled.";
            ApplyState();
        }

        public bool CompleteTimedInteraction(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                _lastInteractionFeedback = "Turret rebuild interrupted.";
                return false;
            }

            FactoryTurret turret = Instantiate(_turretPrefab, _turretMount != null ? _turretMount : transform);
            turret.transform.localPosition = Vector3.zero;
            turret.transform.localRotation = Quaternion.identity;
            turret.name = "Operational Factory Turret";
            _builtTurret = turret;
            _builtTurret.InitializeRuntime(_enemyRegistry, _factoryRegistry, this);
            _lastInteractionFeedback = "Factory turret operational.";
            ApplyState();
            return true;
        }

        public void HandleTurretDestroyed(FactoryTurret turret)
        {
            if (turret == null || turret != _builtTurret)
            {
                return;
            }

            turret.gameObject.SetActive(false);
            _builtTurret = null;
            _lastInteractionFeedback = "Factory turret destroyed. Rebuild available.";
            ApplyState();
        }

        public void RestoreDamagedState()
        {
            if (_builtTurret != null)
            {
                _builtTurret.gameObject.SetActive(false);
                _builtTurret = null;
            }

            ApplyState();
        }

        private void ApplyState()
        {
            bool showBuildState = !IsBuilt;
            if (_damagedInstallation != null)
            {
                _damagedInstallation.SetActive(showBuildState);
            }

            if (_interactionTrigger != null)
            {
                _interactionTrigger.enabled = showBuildState;
            }
        }
    }
}
