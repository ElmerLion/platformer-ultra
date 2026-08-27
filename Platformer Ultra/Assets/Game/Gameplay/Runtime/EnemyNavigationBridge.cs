using System;
using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyNavigationBridge : MonoBehaviour
    {
        [SerializeField] private FactoryObjectiveTerminal _activationTerminal;
        [SerializeField] private Renderer[] _bridgeRenderers = Array.Empty<Renderer>();
        [SerializeField] private Collider[] _bridgeColliders = Array.Empty<Collider>();
        [SerializeField] private Collider[] _playerColliders = Array.Empty<Collider>();
        [SerializeField] private Color _bridgeTint = new Color(0.08f, 0.72f, 0.95f, 1f);
        [SerializeField, Range(0.1f, 0.8f)] private float _visibleAlpha = 0.42f;
        [SerializeField, Min(0.05f)] private float _revealDuration = 1.1f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _propertyBlock;
        private float _currentAlpha;
        private float _targetAlpha;

        public FactoryObjectiveTerminal ActivationTerminal => _activationTerminal;
        public bool IsRevealed => _targetAlpha > 0f;
        public float VisibleAlpha => _visibleAlpha;

        private void OnEnable()
        {
            Subscribe();
            ConfigurePlayerCollision();
            SetRevealState(_activationTerminal != null && _activationTerminal.IsActivated, true);
        }

        private void Start()
        {
            // Terminal Awake order is not guaranteed, so synchronize once all Awake calls have run.
            ConfigurePlayerCollision();
            SetRevealState(_activationTerminal != null && _activationTerminal.IsActivated, true);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            _bridgeRenderers ??= Array.Empty<Renderer>();
            _bridgeColliders ??= Array.Empty<Collider>();
            _playerColliders ??= Array.Empty<Collider>();
            _visibleAlpha = Mathf.Clamp(_visibleAlpha, 0.1f, 0.8f);
            _revealDuration = Mathf.Max(0.05f, _revealDuration);
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentAlpha, _targetAlpha))
            {
                return;
            }

            float speed = _visibleAlpha / _revealDuration;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, speed * Time.deltaTime);
            ApplyAlpha(_currentAlpha);
        }

        public void Configure(
            FactoryObjectiveTerminal activationTerminal,
            Renderer[] bridgeRenderers,
            Collider[] bridgeColliders,
            Collider[] playerColliders,
            float visibleAlpha = 0.42f,
            float revealDuration = 1.1f)
        {
            Unsubscribe();
            _activationTerminal = activationTerminal;
            _bridgeRenderers = bridgeRenderers ?? Array.Empty<Renderer>();
            _bridgeColliders = bridgeColliders ?? Array.Empty<Collider>();
            _playerColliders = playerColliders ?? Array.Empty<Collider>();
            _visibleAlpha = Mathf.Clamp(visibleAlpha, 0.1f, 0.8f);
            _revealDuration = Mathf.Max(0.05f, revealDuration);

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            ConfigurePlayerCollision();
            SetRevealState(_activationTerminal != null && _activationTerminal.IsActivated, true);
        }

        private void HandleTerminalActivated(FactoryObjectiveTerminal terminal)
        {
            SetRevealState(true, !Application.isPlaying);
        }

        private void SetRevealState(bool revealed, bool immediate)
        {
            _targetAlpha = revealed ? _visibleAlpha : 0f;
            if (!immediate)
            {
                ApplyAlpha(_currentAlpha);
                return;
            }

            _currentAlpha = _targetAlpha;
            ApplyAlpha(_currentAlpha);
        }

        private void ApplyAlpha(float alpha)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            Color color = _bridgeTint;
            color.a = alpha;
            bool visible = alpha > 0.001f;

            foreach (Renderer bridgeRenderer in _bridgeRenderers)
            {
                if (bridgeRenderer == null)
                {
                    continue;
                }

                bridgeRenderer.enabled = visible;
                bridgeRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                bridgeRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ConfigurePlayerCollision()
        {
            foreach (Collider playerCollider in _playerColliders)
            {
                if (playerCollider == null)
                {
                    continue;
                }

                foreach (Collider bridgeCollider in _bridgeColliders)
                {
                    if (bridgeCollider != null)
                    {
                        Physics.IgnoreCollision(playerCollider, bridgeCollider, true);
                    }
                }
            }
        }

        private void Subscribe()
        {
            if (_activationTerminal != null)
            {
                _activationTerminal.Activated -= HandleTerminalActivated;
                _activationTerminal.Activated += HandleTerminalActivated;
            }
        }

        private void Unsubscribe()
        {
            if (_activationTerminal != null)
            {
                _activationTerminal.Activated -= HandleTerminalActivated;
            }
        }
    }
}
