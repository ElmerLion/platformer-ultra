using System;
using PlatformerUltra.Combat;
using UnityEngine;


namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(FactionMember), typeof(Targetable))]
    public sealed class FactoryMachineHealth : MonoBehaviour, IDamageable, IFactoryTarget
    {
        [SerializeField] private string _machineName = "Factory Machine";
        [SerializeField, Min(1)] private int _maximumHealth = 120;
        [SerializeField, Range(3f, 8f)] private float _repairDuration = 5f;
        [SerializeField] private Health _health;
        [SerializeField] private FactionMember _factionMember;
        [SerializeField] private Targetable _targetable;
        [SerializeField] private Renderer[] _statusRenderers = Array.Empty<Renderer>();
        [SerializeField] private GameObject _brokenMarker;
        [SerializeField] private FactoryObjectiveTerminal _terminal;
        [SerializeField] private MachineTargetRegistry _registry;
        [SerializeField] private FactoryMachineState _state = FactoryMachineState.Offline;
        [SerializeField] private bool _progressionActivated;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Color _offlineColor = new Color(0.08f, 0.12f, 0.14f);
        private readonly Color _onlineColor = new Color(0.08f, 0.9f, 0.42f);
        private readonly Color _brokenColor = new Color(1f, 0.12f, 0.06f);
        private MaterialPropertyBlock _propertyBlock;

        public string MachineName => _machineName;
        public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;
        public int MaximumHealth => _health != null ? _health.MaximumHealth : _maximumHealth;
        public float RepairDuration => _repairDuration;
        public bool IsAlive => _health != null && _health.IsAlive;
        public bool IsEligibleTarget =>
            _state == FactoryMachineState.Online && IsAlive &&
            _targetable != null && _targetable.IsTargetable;
        public Targetable Targetable => _targetable;
        public FactoryMachineState State => _state;
        public FactoryObjectiveTerminal Terminal => _terminal;
        public GameObject BrokenMarker => _brokenMarker;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<FactoryMachineHealth, FactoryMachineState> StateChanged;
        public event Action<FactoryMachineHealth> Repaired;

        private void Awake()
        {
            ResolveReferences();
            ApplyState();
        }

        private void OnEnable()
        {
            _registry?.Register(this);
        }

        private void OnDisable()
        {
            _registry?.Unregister(this);
        }

        private void OnValidate()
        {
            _machineName = string.IsNullOrWhiteSpace(_machineName) ? "Factory Machine" : _machineName;
            _maximumHealth = Mathf.Max(1, _maximumHealth);
            _repairDuration = Mathf.Clamp(_repairDuration, 3f, 8f);
            _statusRenderers ??= Array.Empty<Renderer>();
            ResolveReferences();
            if (!Application.isPlaying)
            {
                UpdateStatusVisuals();
                UpdateBrokenMarker();
            }
        }

        public void Configure(
            string machineName,
            int maximumHealth,
            float repairDuration,
            Health health,
            FactionMember factionMember,
            Targetable targetable,
            Renderer[] statusRenderers = null,
            GameObject brokenMarker = null)
        {
            _machineName = string.IsNullOrWhiteSpace(machineName) ? "Factory Machine" : machineName;
            _maximumHealth = Mathf.Max(1, maximumHealth);
            _repairDuration = Mathf.Clamp(repairDuration, 3f, 8f);
            _health = health;
            _factionMember = factionMember;
            _targetable = targetable;
            _statusRenderers = statusRenderers ?? Array.Empty<Renderer>();
            _brokenMarker = brokenMarker;
            _progressionActivated = false;
            _state = FactoryMachineState.Offline;

            _health?.Configure(_maximumHealth);
            _factionMember?.Configure(Faction.Factory);
            ApplyState();
        }

        public void BindTerminal(FactoryObjectiveTerminal terminal)
        {
            _terminal = terminal;
            if (_terminal != null && _terminal.MachineHealth != this)
            {
                _terminal.BindMachineHealth(this);
            }

            if (_terminal != null && _terminal.IsActivated)
            {
                SetProgressionActivated();
            }
        }

        public void AssignRegistry(MachineTargetRegistry registry)
        {
            if (_registry == registry)
            {
                if (isActiveAndEnabled)
                {
                    _registry?.Register(this);
                }

                return;
            }

            _registry?.Unregister(this);
            _registry = registry;
            if (isActiveAndEnabled)
            {
                _registry?.Register(this);
            }
        }

        public void SetRepairDuration(float repairDuration)
        {
            _repairDuration = Mathf.Clamp(repairDuration, 3f, 8f);
        }

        public void SetProgressionActivated()
        {
            _progressionActivated = true;
            if (_state != FactoryMachineState.Broken && IsAlive)
            {
                SetState(FactoryMachineState.Online);
            }
        }

        public bool TakeDamage(DamageInfo damageInfo)
        {
            if (_state != FactoryMachineState.Online || _health == null ||
                !_health.TryApplyDamage(damageInfo))
            {
                return false;
            }

            Damaged?.Invoke(damageInfo);
            if (!_health.IsAlive)
            {
                SetState(FactoryMachineState.Broken);
                Died?.Invoke(damageInfo);
            }

            return true;
        }

        public bool TryRepair()
        {
            if (!_progressionActivated || _state != FactoryMachineState.Broken || _health == null)
            {
                return false;
            }

            _health.RestoreFull();
            SetState(FactoryMachineState.Online);
            Repaired?.Invoke(this);
            return true;
        }

        private void SetState(FactoryMachineState state)
        {
            bool changed = _state != state;
            _state = state;
            ApplyState();
            if (changed)
            {
                StateChanged?.Invoke(this, _state);
            }
        }

        private void ApplyState()
        {
            bool targetable = _state == FactoryMachineState.Online && IsAlive;
            _targetable?.SetTargetable(targetable);
            UpdateStatusVisuals();
            UpdateBrokenMarker();
        }

        private void UpdateBrokenMarker()
        {
            if (_brokenMarker != null)
            {
                _brokenMarker.SetActive(_state == FactoryMachineState.Broken);
            }
        }

        private void UpdateStatusVisuals()
        {
            if (_statusRenderers == null || _statusRenderers.Length == 0)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            Color color = _state switch
            {
                FactoryMachineState.Online => _onlineColor,
                FactoryMachineState.Broken => _brokenColor,
                _ => _offlineColor
            };
            Color emission = _state == FactoryMachineState.Offline ? Color.black : color * 2.4f;

            foreach (Renderer statusRenderer in _statusRenderers)
            {
                if (statusRenderer == null)
                {
                    continue;
                }

                statusRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                _propertyBlock.SetColor(EmissionColorId, emission);
                statusRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ResolveReferences()
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }

            if (_factionMember == null)
            {
                _factionMember = GetComponent<FactionMember>();
            }

            if (_targetable == null)
            {
                _targetable = GetComponent<Targetable>();
            }
        }
    }
}
