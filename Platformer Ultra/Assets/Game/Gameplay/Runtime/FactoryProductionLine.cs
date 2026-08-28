using System;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryProductionLine : MonoBehaviour
    {
        [Header("Machines")]
        [SerializeField] private FactoryObjectiveTerminal _mineTerminal;
        [SerializeField] private FactoryObjectiveTerminal _smelterTerminal;
        [SerializeField] private FactoryObjectiveTerminal _assemblerTerminal;

        [Header("Presentation")]
        [SerializeField] private FactoryMachinePresentation _minePresentation;
        [SerializeField] private FactoryMachinePresentation _smelterPresentation;
        [SerializeField] private FactoryMachinePresentation _generatorPresentation;
        [SerializeField] private FactoryMachinePresentation _assemblerPresentation;

        [Header("Connections")]
        [SerializeField] private FactoryConveyorConnection _mineToSmelter;
        [SerializeField] private FactoryConveyorConnection _smelterToAssembler;
        [SerializeField] private FactoryConveyorConnection _assemblerToPortal;

        [Header("Cargo")]
        [SerializeField] private GameObject _oreCargoPrefab;
        [SerializeField] private GameObject _ingotCargoPrefab;
        [SerializeField] private GameObject _portalComponentCargoPrefab;
        [SerializeField] private Transform _cargoRoot;
        [SerializeField] private MonoBehaviour _portalReceiverBehaviour;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float _mineProductionSeconds = 4f;
        [SerializeField, Min(0.1f)] private float _smeltingSeconds = 3f;
        [SerializeField, Min(0.1f)] private float _assemblySeconds = 4f;
        [SerializeField, Min(1)] private int _maximumStoredItems = 3;

        private IFactoryProductionReceiver _portalReceiver;
        private float _mineTimer;
        private float _smelterTimer;
        private float _assemblerTimer;
        private int _storedOre;
        private int _storedIngots;
        private bool _oreInTransit;
        private bool _ingotInTransit;
        private bool _portalComponentInTransit;

        public int StoredOre => _storedOre;
        public int StoredIngots => _storedIngots;
        public int DeliveredPortalComponents => _portalReceiver != null
            ? _portalReceiver.DeliveredCount
            : 0;

        public event Action ProductionChanged;

        private void Awake()
        {
            ResolveReceiver();
        }

        private void OnValidate()
        {
            _mineProductionSeconds = Mathf.Max(0.1f, _mineProductionSeconds);
            _smeltingSeconds = Mathf.Max(0.1f, _smeltingSeconds);
            _assemblySeconds = Mathf.Max(0.1f, _assemblySeconds);
            _maximumStoredItems = Mathf.Max(1, _maximumStoredItems);
            ResolveReceiver();
        }

        private void Update()
        {
            AdvanceProduction(Time.deltaTime);
        }

        private void OnDisable()
        {
            SetPresentationWorkloads(0f, 0f, 0f);
        }

        public void Configure(
            FactoryObjectiveTerminal mineTerminal,
            FactoryObjectiveTerminal smelterTerminal,
            FactoryObjectiveTerminal assemblerTerminal,
            FactoryConveyorConnection mineToSmelter,
            FactoryConveyorConnection smelterToAssembler,
            FactoryConveyorConnection assemblerToPortal,
            GameObject oreCargoPrefab,
            GameObject ingotCargoPrefab,
            GameObject portalComponentCargoPrefab,
            Transform cargoRoot,
            MonoBehaviour portalReceiverBehaviour,
            float mineProductionSeconds = 4f,
            float smeltingSeconds = 3f,
            float assemblySeconds = 4f)
        {
            _mineTerminal = mineTerminal;
            _smelterTerminal = smelterTerminal;
            _assemblerTerminal = assemblerTerminal;
            _mineToSmelter = mineToSmelter;
            _smelterToAssembler = smelterToAssembler;
            _assemblerToPortal = assemblerToPortal;
            _oreCargoPrefab = oreCargoPrefab;
            _ingotCargoPrefab = ingotCargoPrefab;
            _portalComponentCargoPrefab = portalComponentCargoPrefab;
            _cargoRoot = cargoRoot;
            _portalReceiverBehaviour = portalReceiverBehaviour;
            _mineProductionSeconds = Mathf.Max(0.1f, mineProductionSeconds);
            _smeltingSeconds = Mathf.Max(0.1f, smeltingSeconds);
            _assemblySeconds = Mathf.Max(0.1f, assemblySeconds);
            ResetProduction();
            ResolveReceiver();
        }

        public void ResetProduction()
        {
            _mineTimer = 0f;
            _smelterTimer = 0f;
            _assemblerTimer = 0f;
            _storedOre = 0;
            _storedIngots = 0;
            _oreInTransit = false;
            _ingotInTransit = false;
            _portalComponentInTransit = false;
            UpdatePresentationWorkloads();
            ProductionChanged?.Invoke();
        }

        public void BindPresentation(
            FactoryMachinePresentation mine,
            FactoryMachinePresentation smelter,
            FactoryMachinePresentation generator,
            FactoryMachinePresentation assembler)
        {
            _minePresentation = mine;
            _smelterPresentation = smelter;
            _generatorPresentation = generator;
            _assemblerPresentation = assembler;
            UpdatePresentationWorkloads();
        }

        public void AdvanceProduction(float deltaTime)
        {
            if (HasCompletedPortalRequirement())
            {
                SetPresentationWorkloads(0f, 0f, 0f);
                return;
            }

            float step = Mathf.Max(0f, deltaTime);
            AdvanceMine(step);
            AdvanceSmelter(step);
            AdvanceAssembler(step);
            UpdatePresentationWorkloads();
        }

        private void AdvanceMine(float deltaTime)
        {
            if (_oreInTransit || _storedOre >= _maximumStoredItems ||
                _mineToSmelter == null || !_mineToSmelter.IsOperational)
            {
                return;
            }

            _mineTimer += deltaTime;
            if (_mineTimer < _mineProductionSeconds)
            {
                return;
            }

            _mineTimer = 0f;
            _oreInTransit = true;
            _minePresentation?.PlayOutputFeedback();
            SpawnCargo(FactoryCargoKind.Ore, _oreCargoPrefab, _mineToSmelter);
        }

        private void AdvanceSmelter(float deltaTime)
        {
            if (_storedOre <= 0 || _storedIngots >= _maximumStoredItems || _ingotInTransit ||
                _smelterToAssembler == null || !_smelterToAssembler.IsOperational)
            {
                return;
            }

            _smelterTimer += deltaTime;
            if (_smelterTimer < _smeltingSeconds)
            {
                return;
            }

            _smelterTimer = 0f;
            _storedOre--;
            _ingotInTransit = true;
            SpawnCargo(FactoryCargoKind.Ingot, _ingotCargoPrefab, _smelterToAssembler);
            ProductionChanged?.Invoke();
        }

        private void AdvanceAssembler(float deltaTime)
        {
            if (_storedIngots <= 0 || _portalComponentInTransit ||
                _assemblerToPortal == null || !_assemblerToPortal.IsOperational ||
                HasCompletedPortalRequirement())
            {
                return;
            }

            _assemblerTimer += deltaTime;
            if (_assemblerTimer < _assemblySeconds)
            {
                return;
            }

            _assemblerTimer = 0f;
            _storedIngots--;
            _portalComponentInTransit = true;
            _assemblerPresentation?.PlayOutputFeedback();
            SpawnCargo(
                FactoryCargoKind.PortalComponent,
                _portalComponentCargoPrefab,
                _assemblerToPortal);
            ProductionChanged?.Invoke();
        }

        private void SpawnCargo(
            FactoryCargoKind kind,
            GameObject prefab,
            FactoryConveyorConnection connection)
        {
            if (prefab == null)
            {
                HandleCargoArrived(null, kind);
                return;
            }

            GameObject cargoObject = Instantiate(prefab, _cargoRoot != null ? _cargoRoot : transform);
            FactoryProductionCargo cargo = cargoObject.GetComponent<FactoryProductionCargo>();
            if (cargo == null)
            {
                cargo = cargoObject.AddComponent<FactoryProductionCargo>();
            }

            cargo.Configure(kind, connection.Conveyors);
            cargo.Arrived += HandleCargoArrived;
            cargo.Begin();
        }

        private void HandleCargoArrived(FactoryProductionCargo cargo, FactoryCargoKind kind)
        {
            if (cargo != null)
            {
                cargo.Arrived -= HandleCargoArrived;
            }

            switch (kind)
            {
                case FactoryCargoKind.Ore:
                    _oreInTransit = false;
                    _storedOre++;
                    break;
                case FactoryCargoKind.Ingot:
                    _ingotInTransit = false;
                    _storedIngots++;
                    break;
                case FactoryCargoKind.PortalComponent:
                    _portalComponentInTransit = false;
                    _portalReceiver?.ReceivePortalComponent();
                    break;
            }

            ProductionChanged?.Invoke();
        }

        private bool HasCompletedPortalRequirement()
        {
            return _portalReceiver != null &&
                   _portalReceiver.DeliveredCount >= _portalReceiver.RequiredCount;
        }

        private void UpdatePresentationWorkloads()
        {
            float mine = ResolveWorkload(
                !_oreInTransit &&
                _storedOre < _maximumStoredItems &&
                _mineToSmelter != null &&
                _mineToSmelter.IsOperational,
                _mineTimer,
                _mineProductionSeconds);
            float smelter = ResolveWorkload(
                _storedOre > 0 &&
                _storedIngots < _maximumStoredItems &&
                !_ingotInTransit &&
                _smelterToAssembler != null &&
                _smelterToAssembler.IsOperational,
                _smelterTimer,
                _smeltingSeconds);
            float assembler = ResolveWorkload(
                _storedIngots > 0 &&
                !_portalComponentInTransit &&
                _assemblerToPortal != null &&
                _assemblerToPortal.IsOperational &&
                !HasCompletedPortalRequirement(),
                _assemblerTimer,
                _assemblySeconds);
            SetPresentationWorkloads(mine, smelter, assembler);
        }

        private void SetPresentationWorkloads(float mine, float smelter, float assembler)
        {
            _minePresentation?.SetWorkload(mine);
            _smelterPresentation?.SetWorkload(smelter);
            _assemblerPresentation?.SetWorkload(assembler);
            _generatorPresentation?.SetWorkload(Mathf.Max(mine, Mathf.Max(smelter, assembler)));
        }

        private static float ResolveWorkload(bool active, float timer, float duration)
        {
            if (!active)
            {
                return 0f;
            }

            float progress = duration > 0f ? Mathf.Clamp01(timer / duration) : 0f;
            return Mathf.Lerp(0.45f, 1f, progress);
        }

        private void ResolveReceiver()
        {
            _portalReceiver = _portalReceiverBehaviour as IFactoryProductionReceiver;
        }
    }
}
