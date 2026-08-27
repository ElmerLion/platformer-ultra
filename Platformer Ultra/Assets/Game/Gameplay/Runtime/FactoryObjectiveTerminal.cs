using System;
using PlatformerUltra.Factory.Conveyors;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryObjectiveTerminal : MonoBehaviour, ITimedInteractable, IInteractionFeedback
    {
        [SerializeField] private string _stationName = "Factory Station";
        [SerializeField] private FactoryObjectiveTerminal _prerequisite;
        [SerializeField] private Renderer _indicatorRenderer;
        [SerializeField] private GameObject[] _poweredObjects = Array.Empty<GameObject>();
        [SerializeField] private Light[] _workLights = Array.Empty<Light>();
        [SerializeField] private ConveyorBelt[] _conveyors = Array.Empty<ConveyorBelt>();
        [SerializeField] private bool _startActivated;
        [SerializeField] private FactoryMachineHealth _machineHealth;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Color _offlineColor = new Color(0.08f, 0.12f, 0.14f);
        private readonly Color _onlineColor = new Color(0.08f, 0.9f, 0.42f);
        private readonly Color _brokenColor = new Color(1f, 0.12f, 0.06f);
        private MaterialPropertyBlock _propertyBlock;
        private FactoryMachineHealth _subscribedMachineHealth;
        private bool _activated;
        private string _lastInteractionFeedback = string.Empty;

        public bool IsActivated => _activated;
        public bool IsOperational => _activated && MachineState == FactoryMachineState.Online;
        public FactoryMachineState MachineState => !_activated
            ? FactoryMachineState.Offline
            : (_machineHealth != null ? _machineHealth.State : FactoryMachineState.Online);
        public FactoryMachineHealth MachineHealth => _machineHealth;
        public float InteractionDuration => MachineState == FactoryMachineState.Broken && _machineHealth != null
            ? _machineHealth.RepairDuration
            : 0f;
        public string InteractionActionLabel => "Repairing " + _stationName;
        public string LastInteractionFeedback => _lastInteractionFeedback;

        public string InteractionPrompt
        {
            get
            {
                if (!_activated)
                {
                    return "Activate " + _stationName;
                }

                return MachineState == FactoryMachineState.Broken
                    ? $"Hold [E] to Repair {_stationName}"
                    : _stationName + " Online";
            }
        }

        public event Action<FactoryObjectiveTerminal> Activated;
        public event Action<FactoryObjectiveTerminal, FactoryMachineState> MachineStateChanged;

        private void Awake()
        {
            _activated = _startActivated;
            BindConfiguredMachine();
            SubscribeToMachineHealth();
            if (_activated)
            {
                _machineHealth?.SetProgressionActivated();
            }

            ApplyActivationState();
        }

        private void OnEnable()
        {
            SubscribeToMachineHealth();
        }

        private void OnDisable()
        {
            UnsubscribeFromMachineHealth();
        }

        private void OnValidate()
        {
            _stationName = string.IsNullOrWhiteSpace(_stationName) ? "Factory Station" : _stationName;
            _poweredObjects ??= Array.Empty<GameObject>();
            _workLights ??= Array.Empty<Light>();
            _conveyors ??= Array.Empty<ConveyorBelt>();
            if (!Application.isPlaying)
            {
                _activated = _startActivated;
                ApplyActivationState();
            }
        }

        public void Configure(
            string stationName,
            FactoryObjectiveTerminal prerequisite,
            Renderer indicatorRenderer,
            GameObject[] poweredObjects,
            Light[] workLights,
            ConveyorBelt[] conveyors,
            bool startActivated = false,
            FactoryMachineHealth machineHealth = null,
            float repairDuration = 5f)
        {
            _stationName = string.IsNullOrWhiteSpace(stationName) ? "Factory Station" : stationName;
            _prerequisite = prerequisite;
            _indicatorRenderer = indicatorRenderer;
            _poweredObjects = poweredObjects ?? Array.Empty<GameObject>();
            _workLights = workLights ?? Array.Empty<Light>();
            _conveyors = conveyors ?? Array.Empty<ConveyorBelt>();
            _startActivated = startActivated;
            _activated = startActivated;
            BindMachineHealth(machineHealth);
            _machineHealth?.SetRepairDuration(repairDuration);
            if (_activated)
            {
                _machineHealth?.SetProgressionActivated();
            }

            ApplyActivationState();
        }

        public void BindMachineHealth(FactoryMachineHealth machineHealth)
        {
            UnsubscribeFromMachineHealth();
            _machineHealth = machineHealth;
            BindConfiguredMachine();
            if (isActiveAndEnabled)
            {
                SubscribeToMachineHealth();
            }

            if (_activated)
            {
                _machineHealth?.SetProgressionActivated();
            }

            ApplyActivationState();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (!_activated)
            {
                return _prerequisite == null || _prerequisite.IsActivated;
            }

            if (MachineState != FactoryMachineState.Broken)
            {
                return false;
            }

            return interactor != null && _machineHealth != null;
        }

        public void Interact(GameObject interactor)
        {
            _lastInteractionFeedback = string.Empty;
            if (!_activated)
            {
                Activate();
                return;
            }

            if (MachineState != FactoryMachineState.Broken || _machineHealth == null)
            {
                _lastInteractionFeedback = _stationName + " is already online.";
                return;
            }

            _lastInteractionFeedback = "Hold [E] to repair " + _stationName + ".";
        }

        public bool BeginTimedInteraction(GameObject interactor)
        {
            _lastInteractionFeedback = string.Empty;
            return CanInteract(interactor) && MachineState == FactoryMachineState.Broken;
        }

        public void CancelTimedInteraction(GameObject interactor)
        {
            _lastInteractionFeedback = _stationName + " repair cancelled.";
        }

        public bool CompleteTimedInteraction(GameObject interactor)
        {
            if (!CanInteract(interactor) || _machineHealth == null || !_machineHealth.TryRepair())
            {
                _lastInteractionFeedback = _stationName + " repair interrupted.";
                return false;
            }

            _lastInteractionFeedback = _stationName + " repaired.";
            ApplyActivationState();
            return true;
        }

        public void Activate()
        {
            if (_activated)
            {
                _lastInteractionFeedback = MachineState == FactoryMachineState.Broken
                    ? _stationName + " requires repair."
                    : _stationName + " is already online.";
                return;
            }

            if (_prerequisite != null && !_prerequisite.IsActivated)
            {
                _lastInteractionFeedback = $"Activate {_prerequisite._stationName} first.";
                return;
            }

            _activated = true;
            _machineHealth?.SetProgressionActivated();
            ApplyActivationState();
            _lastInteractionFeedback = _stationName + " activated.";
            if (_machineHealth == null)
            {
                MachineStateChanged?.Invoke(this, FactoryMachineState.Online);
            }

            Activated?.Invoke(this);
        }

        private void HandleMachineStateChanged(
            FactoryMachineHealth machine,
            FactoryMachineState state)
        {
            ApplyActivationState();
            MachineStateChanged?.Invoke(this, state);
        }

        private void ApplyActivationState()
        {
            bool operational = IsOperational;
            foreach (GameObject poweredObject in _poweredObjects)
            {
                if (poweredObject != null)
                {
                    poweredObject.SetActive(operational);
                }
            }

            foreach (Light workLight in _workLights)
            {
                if (workLight != null)
                {
                    workLight.enabled = operational;
                }
            }

            ConveyorOperatingState conveyorState = MachineState switch
            {
                FactoryMachineState.Online => ConveyorOperatingState.Online,
                FactoryMachineState.Broken => ConveyorOperatingState.Sabotaged,
                _ => ConveyorOperatingState.Offline
            };
            foreach (ConveyorBelt conveyor in _conveyors)
            {
                if (conveyor != null)
                {
                    conveyor.SetOperatingState(conveyorState);
                }
            }

            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (_indicatorRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            Color color = MachineState switch
            {
                FactoryMachineState.Online => _onlineColor,
                FactoryMachineState.Broken => _brokenColor,
                _ => _offlineColor
            };
            _indicatorRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _propertyBlock.SetColor(EmissionColorId,
                MachineState == FactoryMachineState.Offline ? Color.black : color * 2.4f);
            _indicatorRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void BindConfiguredMachine()
        {
            if (_machineHealth == null)
            {
                return;
            }

            if (_machineHealth.Terminal != this)
            {
                _machineHealth.BindTerminal(this);
            }
        }

        private void SubscribeToMachineHealth()
        {
            if (_subscribedMachineHealth == _machineHealth)
            {
                return;
            }

            UnsubscribeFromMachineHealth();
            if (_machineHealth == null)
            {
                return;
            }

            _machineHealth.StateChanged += HandleMachineStateChanged;
            _subscribedMachineHealth = _machineHealth;
        }

        private void UnsubscribeFromMachineHealth()
        {
            if (_subscribedMachineHealth == null)
            {
                return;
            }

            _subscribedMachineHealth.StateChanged -= HandleMachineStateChanged;
            _subscribedMachineHealth = null;
        }

    }
}
