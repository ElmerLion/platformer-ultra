using System;
using PlatformerUltra.Factory.Conveyors;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    public enum FactoryConveyorConnectionState
    {
        Idle = 0,
        AwaitingDestination = 1,
        Built = 2
    }

    [DisallowMultipleComponent]
    public sealed class FactoryConveyorConnection : MonoBehaviour
    {
        [SerializeField] private string _connectionName = "Factory Conveyor";
        [SerializeField] private FactoryObjectiveTerminal _sourceTerminal;
        [SerializeField] private FactoryObjectiveTerminal _destinationTerminal;
        [SerializeField] private ConveyorBelt[] _conveyors = Array.Empty<ConveyorBelt>();
        [SerializeField] private Renderer _sourceIndicator;
        [SerializeField] private Renderer _destinationIndicator;
        [SerializeField] private GameObject _destinationMarker;
        [SerializeField] private FactoryConveyorConnectionState _state;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _propertyBlock;

        public string ConnectionName => _connectionName;
        public FactoryConveyorConnectionState State => _state;
        public bool IsBuilt => _state == FactoryConveyorConnectionState.Built;
        public bool IsAwaitingDestination => _state == FactoryConveyorConnectionState.AwaitingDestination;
        public bool IsOperational => IsBuilt &&
                                     _sourceTerminal != null && _sourceTerminal.IsOperational &&
                                     (_destinationTerminal == null || _destinationTerminal.IsOperational);
        public ConveyorBelt[] Conveyors => _conveyors;

        public event Action<FactoryConveyorConnection, FactoryConveyorConnectionState> StateChanged;

        private void OnEnable()
        {
            Subscribe();
            ApplyState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            _connectionName = string.IsNullOrWhiteSpace(_connectionName)
                ? "Factory Conveyor"
                : _connectionName;
            _conveyors ??= Array.Empty<ConveyorBelt>();
            if (!Application.isPlaying)
            {
                ApplyState();
            }
        }

        public void Configure(
            string connectionName,
            FactoryObjectiveTerminal sourceTerminal,
            FactoryObjectiveTerminal destinationTerminal,
            ConveyorBelt[] conveyors,
            Renderer sourceIndicator,
            Renderer destinationIndicator,
            GameObject destinationMarker)
        {
            Unsubscribe();
            _connectionName = string.IsNullOrWhiteSpace(connectionName)
                ? "Factory Conveyor"
                : connectionName;
            _sourceTerminal = sourceTerminal;
            _destinationTerminal = destinationTerminal;
            _conveyors = conveyors ?? Array.Empty<ConveyorBelt>();
            _sourceIndicator = sourceIndicator;
            _destinationIndicator = destinationIndicator;
            _destinationMarker = destinationMarker;
            _state = FactoryConveyorConnectionState.Idle;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            ApplyState();
        }

        public bool SelectSource(out string feedback)
        {
            if (IsBuilt)
            {
                feedback = _connectionName + " is already connected.";
                return false;
            }

            if (IsAwaitingDestination)
            {
                SetState(FactoryConveyorConnectionState.Idle);
                feedback = _connectionName + " connection cancelled.";
                return true;
            }

            if (!AreStationsActivated())
            {
                feedback = BuildActivationRequirement();
                return false;
            }

            SetState(FactoryConveyorConnectionState.AwaitingDestination);
            feedback = "Follow the arrow to the destination socket.";
            return true;
        }

        public bool BuildFromDestination(out string feedback)
        {
            if (!IsAwaitingDestination)
            {
                feedback = "Select the source socket first.";
                return false;
            }

            if (!AreStationsActivated())
            {
                SetState(FactoryConveyorConnectionState.Idle);
                feedback = BuildActivationRequirement();
                return false;
            }

            SetState(FactoryConveyorConnectionState.Built);
            feedback = _connectionName + " built.";
            return true;
        }

        private void SetState(FactoryConveyorConnectionState state)
        {
            if (_state == state)
            {
                ApplyState();
                return;
            }

            _state = state;
            ApplyState();
            StateChanged?.Invoke(this, _state);
        }

        private void HandleTerminalActivated(FactoryObjectiveTerminal terminal)
        {
            ApplyState();
        }

        private void HandleMachineStateChanged(
            FactoryObjectiveTerminal terminal,
            FactoryMachineState state)
        {
            ApplyState();
        }

        private void ApplyState()
        {
            bool built = IsBuilt;
            foreach (ConveyorBelt conveyor in _conveyors)
            {
                if (conveyor == null)
                {
                    continue;
                }

                conveyor.gameObject.SetActive(built);
                if (built)
                {
                    conveyor.SetOperatingState(ResolveOperatingState());
                }
            }

            if (_destinationMarker != null)
            {
                _destinationMarker.SetActive(IsAwaitingDestination);
            }

            Color sourceColor = !AreStationsActivated()
                ? new Color(0.95f, 0.28f, 0.04f)
                : IsBuilt
                    ? new Color(0.08f, 0.9f, 0.4f)
                    : IsAwaitingDestination
                        ? new Color(1f, 0.62f, 0.04f)
                        : new Color(0.08f, 0.68f, 0.92f);
            Color destinationColor = IsBuilt
                ? new Color(0.08f, 0.9f, 0.4f)
                : IsAwaitingDestination
                    ? new Color(0.08f, 0.82f, 1f)
                    : new Color(0.08f, 0.12f, 0.15f);
            ApplyIndicator(_sourceIndicator, sourceColor, AreStationsActivated());
            ApplyIndicator(_destinationIndicator, destinationColor, IsAwaitingDestination || IsBuilt);
        }

        private ConveyorOperatingState ResolveOperatingState()
        {
            if (IsOperational)
            {
                return ConveyorOperatingState.Online;
            }

            bool broken = (_sourceTerminal != null &&
                           _sourceTerminal.MachineState == FactoryMachineState.Broken) ||
                          (_destinationTerminal != null &&
                           _destinationTerminal.MachineState == FactoryMachineState.Broken);
            return broken ? ConveyorOperatingState.Sabotaged : ConveyorOperatingState.Offline;
        }

        private bool AreStationsActivated()
        {
            return _sourceTerminal != null && _sourceTerminal.IsActivated &&
                   (_destinationTerminal == null || _destinationTerminal.IsActivated);
        }

        private string BuildActivationRequirement()
        {
            if (_sourceTerminal == null || !_sourceTerminal.IsActivated)
            {
                return "Activate the source machine first.";
            }

            return "Activate the destination machine first.";
        }

        private void ApplyIndicator(Renderer indicator, Color color, bool emissive)
        {
            if (indicator == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            indicator.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _propertyBlock.SetColor(EmissionColorId, emissive ? color * 2.5f : Color.black);
            indicator.SetPropertyBlock(_propertyBlock);
        }

        private void Subscribe()
        {
            Subscribe(_sourceTerminal);
            Subscribe(_destinationTerminal);
        }

        private void Unsubscribe()
        {
            Unsubscribe(_sourceTerminal);
            Unsubscribe(_destinationTerminal);
        }

        private void Subscribe(FactoryObjectiveTerminal terminal)
        {
            if (terminal == null)
            {
                return;
            }

            terminal.Activated -= HandleTerminalActivated;
            terminal.MachineStateChanged -= HandleMachineStateChanged;
            terminal.Activated += HandleTerminalActivated;
            terminal.MachineStateChanged += HandleMachineStateChanged;
        }

        private void Unsubscribe(FactoryObjectiveTerminal terminal)
        {
            if (terminal == null)
            {
                return;
            }

            terminal.Activated -= HandleTerminalActivated;
            terminal.MachineStateChanged -= HandleMachineStateChanged;
        }
    }
}
