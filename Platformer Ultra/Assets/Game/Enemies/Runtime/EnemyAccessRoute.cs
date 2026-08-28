using System;
using PlatformerUltra.Gameplay;
using Unity.AI.Navigation;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAccessRoute : MonoBehaviour
    {
        [SerializeField] private FactoryObjectiveTerminal _activationTerminal;
        [SerializeField] private EnemyTraversalLink[] _traversalLinks = Array.Empty<EnemyTraversalLink>();
        [SerializeField] private Transform[] _deploymentParts = Array.Empty<Transform>();
        [SerializeField] private Vector3[] _retractedLocalPositions = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] _deployedLocalPositions = Array.Empty<Vector3>();
        [SerializeField, Min(0.05f)] private float _deploymentDuration = 1.35f;

        private float _deploymentProgress;
        private bool _deploying;
        private bool _subscribed;

        public FactoryObjectiveTerminal ActivationTerminal => _activationTerminal;
        public float DeploymentDuration => _deploymentDuration;
        public float DeploymentProgress => _deploymentProgress;
        public bool IsDeployed => _deploymentProgress >= 1f && LinksEnabled;
        public bool LinksEnabled
        {
            get
            {
                if (_traversalLinks == null || _traversalLinks.Length == 0)
                {
                    return false;
                }

                for (int index = 0; index < _traversalLinks.Length; index++)
                {
                    EnemyTraversalLink traversal = _traversalLinks[index];
                    if (traversal == null || traversal.Link == null || !traversal.Link.enabled)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void OnEnable()
        {
            Subscribe();
            bool activated = _activationTerminal == null || _activationTerminal.IsActivated;
            SetDeploymentProgress(activated ? 1f : 0f, activated);
            _deploying = false;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            _traversalLinks ??= Array.Empty<EnemyTraversalLink>();
            _deploymentParts ??= Array.Empty<Transform>();
            _retractedLocalPositions ??= Array.Empty<Vector3>();
            _deployedLocalPositions ??= Array.Empty<Vector3>();
            _deploymentDuration = Mathf.Max(0.05f, _deploymentDuration);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(
            FactoryObjectiveTerminal activationTerminal,
            EnemyTraversalLink[] traversalLinks,
            Transform[] deploymentParts,
            Vector3[] retractedLocalPositions,
            float deploymentDuration = 1.35f)
        {
            Unsubscribe();
            _activationTerminal = activationTerminal;
            _traversalLinks = traversalLinks ?? Array.Empty<EnemyTraversalLink>();
            _deploymentParts = deploymentParts ?? Array.Empty<Transform>();
            _retractedLocalPositions = retractedLocalPositions ?? Array.Empty<Vector3>();
            _deploymentDuration = Mathf.Max(0.05f, deploymentDuration);
            _deployedLocalPositions = new Vector3[_deploymentParts.Length];
            for (int index = 0; index < _deploymentParts.Length; index++)
            {
                Transform part = _deploymentParts[index];
                _deployedLocalPositions[index] = part != null ? part.localPosition : Vector3.zero;
            }

            if (_retractedLocalPositions.Length != _deploymentParts.Length)
            {
                Array.Resize(ref _retractedLocalPositions, _deploymentParts.Length);
                for (int index = 0; index < _deploymentParts.Length; index++)
                {
                    _retractedLocalPositions[index] = _deployedLocalPositions[index];
                }
            }

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            bool activated = _activationTerminal == null || _activationTerminal.IsActivated;
            SetDeploymentProgress(activated ? 1f : 0f, activated);
            _deploying = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_deploying || IsDeployed)
            {
                return;
            }

            float next = Mathf.MoveTowards(
                _deploymentProgress,
                1f,
                Mathf.Max(0f, deltaTime) / _deploymentDuration);
            bool completed = next >= 1f;
            SetDeploymentProgress(next, completed);
            _deploying = !completed;
        }

        private void HandleTerminalActivated(FactoryObjectiveTerminal terminal)
        {
            if (IsDeployed)
            {
                return;
            }

            _deploying = true;
            SetLinksEnabled(false);
        }

        private void SetDeploymentProgress(float progress, bool enableLinks)
        {
            _deploymentProgress = Mathf.Clamp01(progress);
            float eased = _deploymentProgress * _deploymentProgress * (3f - 2f * _deploymentProgress);
            int count = Mathf.Min(
                _deploymentParts.Length,
                Mathf.Min(_retractedLocalPositions.Length, _deployedLocalPositions.Length));
            for (int index = 0; index < count; index++)
            {
                Transform part = _deploymentParts[index];
                if (part != null)
                {
                    float stagger = count <= 1 ? 0f : index / (float)(count - 1) * 0.16f;
                    float partProgress = Mathf.InverseLerp(stagger, 1f, eased);
                    part.localPosition = Vector3.LerpUnclamped(
                        _retractedLocalPositions[index],
                        _deployedLocalPositions[index],
                        partProgress);
                }
            }

            SetLinksEnabled(enableLinks && _deploymentProgress >= 1f);
        }

        private void SetLinksEnabled(bool enabled)
        {
            for (int index = 0; index < _traversalLinks.Length; index++)
            {
                EnemyTraversalLink traversal = _traversalLinks[index];
                NavMeshLink link = traversal != null ? traversal.Link : null;
                if (link == null)
                {
                    continue;
                }

                link.enabled = enabled;
                if (enabled && link.isActiveAndEnabled)
                {
                    link.UpdateLink();
                }
            }
        }

        private void Subscribe()
        {
            if (_subscribed || _activationTerminal == null)
            {
                return;
            }

            _activationTerminal.Activated += HandleTerminalActivated;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _activationTerminal == null)
            {
                return;
            }

            _activationTerminal.Activated -= HandleTerminalActivated;
            _subscribed = false;
        }
    }
}
