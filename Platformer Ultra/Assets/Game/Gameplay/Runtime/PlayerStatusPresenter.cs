using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlayerStatusPresenter : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _styleSheet;
        [SerializeField] private PlayerHealth _playerHealth;

        private Label _healthValue;
        private ProgressBar _healthProgress;
        private VisualElement _gameOverPanel;
        private Button _retryButton;
        private VisualElement _crosshair;
        private VisualElement _styledRoot;
        private bool _healthBound;
        private bool _buttonBound;

        public bool IsGameOverVisible { get; private set; }

        public event Action RetryRequested;

        private void Awake()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            ResolveElements();
        }

        private void OnEnable()
        {
            ResolveElements();
            BindHealth();
            BindRetryButton();
            HideGameOver();
            RefreshHealth();
        }

        private void OnDisable()
        {
            UnbindHealth();
            UnbindRetryButton();
        }

        public void Configure(
            UIDocument document,
            StyleSheet styleSheet,
            PlayerHealth playerHealth)
        {
            UnbindHealth();
            UnbindRetryButton();
            _document = document;
            _styleSheet = styleSheet;
            _playerHealth = playerHealth;
            ResolveElements();
            BindHealth();
            BindRetryButton();
            HideGameOver();
            RefreshHealth();
        }

        public void ShowGameOver()
        {
            ResolveElements();
            IsGameOverVisible = true;
            if (_gameOverPanel != null)
            {
                _gameOverPanel.style.display = DisplayStyle.Flex;
            }

            if (_crosshair != null)
            {
                _crosshair.style.display = DisplayStyle.None;
            }
        }

        public void HideGameOver()
        {
            ResolveElements();
            IsGameOverVisible = false;
            if (_gameOverPanel != null)
            {
                _gameOverPanel.style.display = DisplayStyle.None;
            }

            if (_crosshair != null)
            {
                _crosshair.style.display = DisplayStyle.Flex;
            }
        }

        public void RequestRetry()
        {
            RetryRequested?.Invoke();
        }

        private void HandleHealthChanged(int currentHealth, int maximumHealth)
        {
            SetHealth(currentHealth, maximumHealth);
        }

        private void RefreshHealth()
        {
            if (_playerHealth == null)
            {
                return;
            }

            SetHealth(_playerHealth.CurrentHealth, _playerHealth.MaximumHealth);
        }

        private void SetHealth(int currentHealth, int maximumHealth)
        {
            ResolveElements();
            int safeMaximum = Mathf.Max(1, maximumHealth);
            int safeCurrent = Mathf.Clamp(currentHealth, 0, safeMaximum);
            if (_healthValue != null)
            {
                _healthValue.text = safeCurrent + " / " + safeMaximum;
            }

            if (_healthProgress != null)
            {
                _healthProgress.highValue = safeMaximum;
                _healthProgress.value = safeCurrent;
            }
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null)
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            if (_styleSheet != null && _styledRoot != root)
            {
                root.styleSheets.Add(_styleSheet);
                _styledRoot = root;
            }

            _healthValue = root.Q<Label>("player-health-value");
            _healthProgress = root.Q<ProgressBar>("player-health-progress");
            _gameOverPanel = root.Q<VisualElement>("game-over-panel");
            _retryButton = root.Q<Button>("retry-button");
            _crosshair = root.Q<VisualElement>(className: "crosshair");
        }

        private void BindHealth()
        {
            if (_healthBound || _playerHealth == null || !isActiveAndEnabled)
            {
                return;
            }

            _playerHealth.HealthChanged += HandleHealthChanged;
            _healthBound = true;
        }

        private void UnbindHealth()
        {
            if (!_healthBound || _playerHealth == null)
            {
                _healthBound = false;
                return;
            }

            _playerHealth.HealthChanged -= HandleHealthChanged;
            _healthBound = false;
        }

        private void BindRetryButton()
        {
            if (_buttonBound || _retryButton == null || !isActiveAndEnabled)
            {
                return;
            }

            _retryButton.clicked += RequestRetry;
            _buttonBound = true;
        }

        private void UnbindRetryButton()
        {
            if (_buttonBound && _retryButton != null)
            {
                _retryButton.clicked -= RequestRetry;
            }

            _buttonBound = false;
        }
    }
}
