using System;
using PlatformerUltra.Gameplay;
using UnityEngine;

namespace PlatformerUltra.Factory
{
    [DisallowMultipleComponent]
    public sealed class FactoryPortalGate : MonoBehaviour, IFactoryProductionReceiver
    {
        [SerializeField] private FactoryPortalVisual _portalVisual;
        [SerializeField] private GameObject _approachBridge;
        [SerializeField] private Renderer[] _socketIndicators = Array.Empty<Renderer>();
        [SerializeField] private GameObject[] _installedCoreVisuals = Array.Empty<GameObject>();
        [SerializeField, Min(1)] private int _requiredCoreCount = 1;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private bool[] _collectedSockets;
        private MaterialPropertyBlock _propertyBlock;
        private int _collectedCount;

        public int CollectedCount => _collectedCount;
        public int RequiredCoreCount => _requiredCoreCount;
        public bool IsOpen => _collectedCount >= _requiredCoreCount;
        public int DeliveredCount => _collectedCount;
        public int RequiredCount => _requiredCoreCount;

        public event Action<int, int> ProgressChanged;

        private void Awake()
        {
            ResetGate();
        }

        public void Configure(
            FactoryPortalVisual portalVisual,
            GameObject approachBridge,
            Renderer[] socketIndicators,
            int requiredCoreCount = 1)
        {
            Configure(portalVisual, approachBridge, socketIndicators, null, requiredCoreCount);
        }

        public void Configure(
            FactoryPortalVisual portalVisual,
            GameObject approachBridge,
            Renderer[] socketIndicators,
            GameObject[] installedCoreVisuals,
            int requiredCoreCount = 1)
        {
            _portalVisual = portalVisual;
            _approachBridge = approachBridge;
            _socketIndicators = socketIndicators ?? Array.Empty<Renderer>();
            _installedCoreVisuals = installedCoreVisuals ?? Array.Empty<GameObject>();
            _requiredCoreCount = Mathf.Max(1, requiredCoreCount);
        }

        public void CollectCore(int socketIndex)
        {
            EnsureSocketState();
            if (socketIndex < 0 || socketIndex >= _collectedSockets.Length || _collectedSockets[socketIndex])
            {
                return;
            }

            _collectedSockets[socketIndex] = true;
            _collectedCount++;
            UpdateSocketIndicators();
            UpdateInstalledCoreVisuals();
            ProgressChanged?.Invoke(_collectedCount, _requiredCoreCount);

            if (_collectedCount < _requiredCoreCount)
            {
                return;
            }

            if (_approachBridge != null)
            {
                _approachBridge.SetActive(true);
            }

            _portalVisual?.SetState(FactoryPortalState.Activating);
        }

        public void ReceivePortalComponent()
        {
            EnsureSocketState();
            for (int index = 0; index < _collectedSockets.Length; index++)
            {
                if (!_collectedSockets[index])
                {
                    CollectCore(index);
                    return;
                }
            }
        }

        public void ResetGate()
        {
            _collectedCount = 0;
            EnsureSocketState();
            Array.Clear(_collectedSockets, 0, _collectedSockets.Length);

            if (_approachBridge != null)
            {
                _approachBridge.SetActive(false);
            }

            _portalVisual?.SetState(FactoryPortalState.Inactive);
            UpdateSocketIndicators();
            UpdateInstalledCoreVisuals();
            ProgressChanged?.Invoke(_collectedCount, _requiredCoreCount);
        }

        private void EnsureSocketState()
        {
            int capacity = Mathf.Max(_requiredCoreCount, _socketIndicators != null ? _socketIndicators.Length : 0);
            if (_collectedSockets == null || _collectedSockets.Length != capacity)
            {
                _collectedSockets = new bool[capacity];
            }
        }

        private void UpdateSocketIndicators()
        {
            if (_socketIndicators == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            for (int index = 0; index < _socketIndicators.Length; index++)
            {
                Renderer indicator = _socketIndicators[index];
                if (indicator == null)
                {
                    continue;
                }

                bool active = index < _collectedSockets.Length && _collectedSockets[index];
                Color color = active
                    ? new Color(0.06f, 0.88f, 1f)
                    : new Color(0.08f, 0.12f, 0.15f);
                indicator.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                _propertyBlock.SetColor(EmissionColorId, active ? color * 3f : Color.black);
                indicator.SetPropertyBlock(_propertyBlock);
            }
        }

        private void UpdateInstalledCoreVisuals()
        {
            if (_installedCoreVisuals == null)
            {
                return;
            }

            for (int index = 0; index < _installedCoreVisuals.Length; index++)
            {
                GameObject visual = _installedCoreVisuals[index];
                if (visual != null)
                {
                    visual.SetActive(index < _collectedSockets.Length && _collectedSockets[index]);
                }
            }
        }
    }
}
