using System;
using PlatformerUltra.Audio;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    public enum FactoryMachinePresentationState
    {
        Offline = 0,
        Starting = 1,
        OnlineIdle = 2,
        Working = 3,
        Broken = 4
    }

    public enum FactoryMachinePresentationKind
    {
        Mine = 0,
        Smelter = 1,
        Generator = 2,
        Assembler = 3
    }

    [DisallowMultipleComponent]
    public sealed class FactoryMachinePresentation : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private FactoryMachinePresentationKind _kind;
        [SerializeField] private FactoryMachineHealth _machineHealth;
        [SerializeField, Min(0.1f)] private float _startupDuration = 1.1f;

        [Header("Motion")]
        [SerializeField] private Transform _motionRoot;
        [SerializeField] private Transform[] _rotors = Array.Empty<Transform>();
        [SerializeField] private Transform[] _pistons = Array.Empty<Transform>();
        [SerializeField] private Transform[] _pulseParts = Array.Empty<Transform>();
        [SerializeField] private Transform[] _articulatedParts = Array.Empty<Transform>();
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;
        [SerializeField] private Vector3 _pistonAxis = Vector3.up;
        [SerializeField] private float _idleRotationSpeed = 12f;
        [SerializeField] private float _workingRotationSpeed = 120f;
        [SerializeField, Min(0f)] private float _pistonTravel = 0.08f;
        [SerializeField, Min(0f)] private float _workingVibration = 0.025f;

        [Header("Feedback")]
        [SerializeField] private Renderer[] _emissiveRenderers = Array.Empty<Renderer>();
        [SerializeField] private Color _emissionColor = new Color(0.08f, 0.78f, 1f);
        [SerializeField] private MachineLoopAudio[] _audioLoops = Array.Empty<MachineLoopAudio>();
        [SerializeField] private ParticleSystem[] _startupEffects = Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _idleEffects = Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _workingEffects = Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] _outputEffects = Array.Empty<ParticleSystem>();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Vector3 _motionRootPosition;
        private Vector3[] _pistonPositions = Array.Empty<Vector3>();
        private Vector3[] _pulseScales = Array.Empty<Vector3>();
        private Quaternion[] _articulatedRotations = Array.Empty<Quaternion>();
        private MaterialPropertyBlock _propertyBlock;
        private FactoryMachinePresentationState _state;
        private float _startupElapsed;
        private float _workload;
        private float _motionTime;
        private bool _restPoseCached;

        public FactoryMachinePresentationState State => _state;
        public float Workload => _workload;

        public void Configure(
            FactoryMachinePresentationKind kind,
            FactoryMachineHealth machineHealth,
            Transform motionRoot,
            Transform[] rotors,
            Transform[] pistons,
            Transform[] pulseParts,
            Transform[] articulatedParts,
            Renderer[] emissiveRenderers,
            Color emissionColor,
            MachineLoopAudio[] audioLoops,
            ParticleSystem[] startupEffects,
            ParticleSystem[] idleEffects,
            ParticleSystem[] workingEffects,
            Vector3 rotationAxis,
            Vector3 pistonAxis,
            float idleRotationSpeed,
            float workingRotationSpeed,
            float pistonTravel,
            float workingVibration,
            float startupDuration = 1.1f,
            ParticleSystem[] outputEffects = null)
        {
            Unsubscribe();
            _kind = kind;
            _machineHealth = machineHealth;
            _motionRoot = motionRoot;
            _rotors = rotors ?? Array.Empty<Transform>();
            _pistons = pistons ?? Array.Empty<Transform>();
            _pulseParts = pulseParts ?? Array.Empty<Transform>();
            _articulatedParts = articulatedParts ?? Array.Empty<Transform>();
            _emissiveRenderers = emissiveRenderers ?? Array.Empty<Renderer>();
            _emissionColor = emissionColor;
            _audioLoops = audioLoops ?? Array.Empty<MachineLoopAudio>();
            _startupEffects = startupEffects ?? Array.Empty<ParticleSystem>();
            _idleEffects = idleEffects ?? Array.Empty<ParticleSystem>();
            _workingEffects = workingEffects ?? Array.Empty<ParticleSystem>();
            _outputEffects = outputEffects ?? Array.Empty<ParticleSystem>();
            _rotationAxis = rotationAxis.sqrMagnitude > 0.001f ? rotationAxis.normalized : Vector3.up;
            _pistonAxis = pistonAxis.sqrMagnitude > 0.001f ? pistonAxis.normalized : Vector3.up;
            _idleRotationSpeed = idleRotationSpeed;
            _workingRotationSpeed = workingRotationSpeed;
            _pistonTravel = Mathf.Max(0f, pistonTravel);
            _workingVibration = Mathf.Max(0f, workingVibration);
            _startupDuration = Mathf.Max(0.1f, startupDuration);
            CacheRestPose();
            Subscribe();
            ApplyMachineState(false);
        }

        public void SetWorkload(float normalized)
        {
            _workload = Mathf.Clamp01(normalized);
            if (_state == FactoryMachinePresentationState.OnlineIdle ||
                _state == FactoryMachinePresentationState.Working)
            {
                SetPresentationState(_workload > 0.05f
                    ? FactoryMachinePresentationState.Working
                    : FactoryMachinePresentationState.OnlineIdle);
            }

            UpdateAudioIntensity();
        }

        public void PlayOutputFeedback()
        {
            if (_state == FactoryMachinePresentationState.Working)
            {
                PlayOneShots(_outputEffects);
            }
        }

        private void Awake()
        {
            CacheRestPose();
            ApplyMachineState(false);
        }

        private void OnEnable()
        {
            Subscribe();
            ApplyMachineState(false);
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetLoopsPlaying(_idleEffects, false);
            SetLoopsPlaying(_workingEffects, false);
            SetAudioPlaying(false);
        }

        private void OnValidate()
        {
            _startupDuration = Mathf.Max(0.1f, _startupDuration);
            _pistonTravel = Mathf.Max(0f, _pistonTravel);
            _workingVibration = Mathf.Max(0f, _workingVibration);
            _rotors ??= Array.Empty<Transform>();
            _pistons ??= Array.Empty<Transform>();
            _pulseParts ??= Array.Empty<Transform>();
            _articulatedParts ??= Array.Empty<Transform>();
            _emissiveRenderers ??= Array.Empty<Renderer>();
            _audioLoops ??= Array.Empty<MachineLoopAudio>();
            _startupEffects ??= Array.Empty<ParticleSystem>();
            _idleEffects ??= Array.Empty<ParticleSystem>();
            _workingEffects ??= Array.Empty<ParticleSystem>();
            _outputEffects ??= Array.Empty<ParticleSystem>();
        }

        private void Update()
        {
            AdvancePresentation(Time.deltaTime);
        }

        public void AdvancePresentation(float deltaTime)
        {
            if (_state == FactoryMachinePresentationState.Offline ||
                _state == FactoryMachinePresentationState.Broken)
            {
                return;
            }

            deltaTime = Mathf.Max(0f, deltaTime);
            _motionTime += deltaTime;
            if (_state == FactoryMachinePresentationState.Starting)
            {
                _startupElapsed += deltaTime;
                if (_startupElapsed >= _startupDuration)
                {
                    SetPresentationState(_workload > 0.05f
                        ? FactoryMachinePresentationState.Working
                        : FactoryMachinePresentationState.OnlineIdle);
                }
            }

            ApplyMotion(deltaTime);
            UpdateEmission();
            UpdateAudioIntensity();
        }

        private void HandleMachineStateChanged(FactoryMachineHealth machine, FactoryMachineState state)
        {
            ApplyMachineState(true);
        }

        private void ApplyMachineState(bool allowStartup)
        {
            FactoryMachineState machineState = _machineHealth != null
                ? _machineHealth.State
                : FactoryMachineState.Offline;
            if (machineState == FactoryMachineState.Broken)
            {
                SetPresentationState(FactoryMachinePresentationState.Broken);
                return;
            }

            if (machineState == FactoryMachineState.Offline)
            {
                SetPresentationState(FactoryMachinePresentationState.Offline);
                return;
            }

            SetPresentationState(allowStartup
                ? FactoryMachinePresentationState.Starting
                : (_workload > 0.05f
                    ? FactoryMachinePresentationState.Working
                    : FactoryMachinePresentationState.OnlineIdle));
        }

        private void SetPresentationState(FactoryMachinePresentationState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            if (state == FactoryMachinePresentationState.Starting)
            {
                _startupElapsed = 0f;
                PlayOneShots(_startupEffects);
            }

            bool online = state == FactoryMachinePresentationState.Starting ||
                          state == FactoryMachinePresentationState.OnlineIdle ||
                          state == FactoryMachinePresentationState.Working;
            SetAudioPlaying(online);
            UpdateAudioIntensity();
            SetLoopsPlaying(_idleEffects, online);
            SetLoopsPlaying(_workingEffects, state == FactoryMachinePresentationState.Working);

            if (!online)
            {
                ResetRestPose();
            }

            UpdateEmission();
        }

        private void ApplyMotion(float deltaTime)
        {
            float startup = _state == FactoryMachinePresentationState.Starting
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_startupElapsed / _startupDuration))
                : 1f;
            float intensity = Mathf.Lerp(0.18f, 1f, _workload) * startup;
            float rotationSpeed = Mathf.Lerp(_idleRotationSpeed, _workingRotationSpeed, _workload) * startup;
            for (int index = 0; index < _rotors.Length; index++)
            {
                Transform rotor = _rotors[index];
                if (rotor == null)
                {
                    continue;
                }

                float direction = index % 2 == 0 ? 1f : -1f;
                rotor.Rotate(_rotationAxis, rotationSpeed * direction * deltaTime, Space.Self);
            }

            float frequency = Mathf.Lerp(1.4f, 4.8f, _workload);
            float wave = Mathf.Sin(_motionTime * frequency * Mathf.PI * 2f);
            for (int index = 0; index < _pistons.Length && index < _pistonPositions.Length; index++)
            {
                Transform piston = _pistons[index];
                if (piston != null)
                {
                    float phase = index % 2 == 0 ? wave : -wave;
                    piston.localPosition = _pistonPositions[index] + _pistonAxis * (_pistonTravel * phase * intensity);
                }
            }

            for (int index = 0; index < _pulseParts.Length && index < _pulseScales.Length; index++)
            {
                Transform pulsePart = _pulseParts[index];
                if (pulsePart != null)
                {
                    float pulse = 1f + (0.025f + _workload * 0.045f) *
                        Mathf.Sin(_motionTime * (3f + index * 0.35f) + index * 0.8f) * startup;
                    pulsePart.localScale = _pulseScales[index] * pulse;
                }
            }

            ApplyArticulation(startup);
            if (_motionRoot != null)
            {
                float vibration = _workingVibration * _workload * startup;
                _motionRoot.localPosition = _motionRootPosition + new Vector3(
                    Mathf.Sin(_motionTime * 27f) * vibration,
                    Mathf.Sin(_motionTime * 31f) * vibration * 0.55f,
                    0f);
            }
        }

        private void ApplyArticulation(float startup)
        {
            if (_articulatedParts.Length == 0)
            {
                return;
            }

            float cycle = _motionTime * Mathf.Lerp(0.65f, 1.45f, _workload);
            for (int index = 0; index < _articulatedParts.Length && index < _articulatedRotations.Length; index++)
            {
                Transform part = _articulatedParts[index];
                if (part == null)
                {
                    continue;
                }

                float direction = index % 2 == 0 ? 1f : -1f;
                float angle;
                Vector3 axis;
                switch (_kind)
                {
                    case FactoryMachinePresentationKind.Assembler:
                        angle = direction * (4f + _workload * 24f) * Mathf.Sin(cycle + index * 0.65f) * startup;
                        axis = Vector3.forward;
                        break;
                    case FactoryMachinePresentationKind.Smelter:
                        angle = direction * (2f + _workload * 5f) * Mathf.Sin(cycle * 1.7f) * startup;
                        axis = Vector3.right;
                        break;
                    default:
                        angle = direction * _workload * 3f * Mathf.Sin(cycle) * startup;
                        axis = Vector3.forward;
                        break;
                }

                part.localRotation = _articulatedRotations[index] * Quaternion.AngleAxis(angle, axis);
            }
        }

        private void UpdateEmission()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            float intensity = _state switch
            {
                FactoryMachinePresentationState.Starting => Mathf.Lerp(
                    0.2f,
                    3.4f,
                    Mathf.Clamp01(_startupElapsed / _startupDuration)),
                FactoryMachinePresentationState.OnlineIdle => 1.25f + Mathf.Sin(_motionTime * 3.2f) * 0.18f,
                FactoryMachinePresentationState.Working => 2.2f + Mathf.Sin(_motionTime * 6.5f) * 0.35f,
                FactoryMachinePresentationState.Broken => 1.8f,
                _ => 0f
            };
            Color activeColor = _state == FactoryMachinePresentationState.Broken
                ? new Color(1f, 0.12f, 0.06f)
                : _emissionColor;
            Color baseColor = intensity > 0f ? activeColor : new Color(0.05f, 0.07f, 0.08f);
            foreach (Renderer targetRenderer in _emissiveRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, baseColor);
                _propertyBlock.SetColor(ColorId, baseColor);
                _propertyBlock.SetColor(EmissionColorId, activeColor * intensity);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void CacheRestPose()
        {
            _motionRootPosition = _motionRoot != null ? _motionRoot.localPosition : Vector3.zero;
            _pistonPositions = new Vector3[_pistons.Length];
            for (int index = 0; index < _pistons.Length; index++)
            {
                _pistonPositions[index] = _pistons[index] != null ? _pistons[index].localPosition : Vector3.zero;
            }

            _pulseScales = new Vector3[_pulseParts.Length];
            for (int index = 0; index < _pulseParts.Length; index++)
            {
                _pulseScales[index] = _pulseParts[index] != null ? _pulseParts[index].localScale : Vector3.one;
            }

            _articulatedRotations = new Quaternion[_articulatedParts.Length];
            for (int index = 0; index < _articulatedParts.Length; index++)
            {
                _articulatedRotations[index] = _articulatedParts[index] != null
                    ? _articulatedParts[index].localRotation
                    : Quaternion.identity;
            }

            _restPoseCached = true;
        }

        private void ResetRestPose()
        {
            if (!_restPoseCached)
            {
                CacheRestPose();
            }

            if (_motionRoot != null)
            {
                _motionRoot.localPosition = _motionRootPosition;
            }

            for (int index = 0; index < _pistons.Length && index < _pistonPositions.Length; index++)
            {
                if (_pistons[index] != null)
                {
                    _pistons[index].localPosition = _pistonPositions[index];
                }
            }

            for (int index = 0; index < _pulseParts.Length && index < _pulseScales.Length; index++)
            {
                if (_pulseParts[index] != null)
                {
                    _pulseParts[index].localScale = _pulseScales[index];
                }
            }

            for (int index = 0; index < _articulatedParts.Length && index < _articulatedRotations.Length; index++)
            {
                if (_articulatedParts[index] != null)
                {
                    _articulatedParts[index].localRotation = _articulatedRotations[index];
                }
            }
        }

        private void Subscribe()
        {
            if (_machineHealth == null)
            {
                return;
            }

            _machineHealth.StateChanged -= HandleMachineStateChanged;
            _machineHealth.StateChanged += HandleMachineStateChanged;
        }

        private void Unsubscribe()
        {
            if (_machineHealth != null)
            {
                _machineHealth.StateChanged -= HandleMachineStateChanged;
            }
        }

        private void SetAudioPlaying(bool shouldPlay)
        {
            foreach (MachineLoopAudio loop in _audioLoops)
            {
                loop?.SetPlaying(shouldPlay);
            }
        }

        private void UpdateAudioIntensity()
        {
            float intensity = _state switch
            {
                FactoryMachinePresentationState.Starting => Mathf.Lerp(
                    0.22f,
                    0.68f,
                    Mathf.Clamp01(_startupElapsed / _startupDuration)),
                FactoryMachinePresentationState.OnlineIdle => 0.35f,
                FactoryMachinePresentationState.Working => Mathf.Lerp(0.55f, 1f, _workload),
                _ => 0f
            };
            foreach (MachineLoopAudio loop in _audioLoops)
            {
                loop?.SetIntensity(intensity);
            }
        }

        private static void PlayOneShots(ParticleSystem[] effects)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            foreach (ParticleSystem effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.Play(true);
            }
        }

        private static void SetLoopsPlaying(ParticleSystem[] effects, bool shouldPlay)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            foreach (ParticleSystem effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                if (shouldPlay && !effect.isPlaying)
                {
                    effect.Play(true);
                }
                else if (!shouldPlay && effect.isPlaying)
                {
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }
}
