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
        private VisualElement _pausePanel;
        private Button _resumeButton;
        private Button _pauseRetryButton;
        private VisualElement _victoryPanel;
        private Button _victoryRetryButton;
        private VisualElement _crosshair;
        private VisualElement _objectiveCard;
        private VisualElement _portalCoreCard;
        private VisualElement _tutorialTip;
        private VisualElement _healthPanel;
        private VisualElement _interactionPrompt;
        private VisualElement _timedInteractionPanel;
        private VisualElement _styledRoot;
        private bool _healthBound;
        private bool _buttonBound;
        private bool _gameplayHudHidden;

        public bool IsGameOverVisible { get; private set; }
        public bool IsPauseVisible { get; private set; }
        public bool IsVictoryVisible { get; private set; }

        public event Action RetryRequested;
        public event Action ResumeRequested;
        public event Action PauseRetryRequested;
        public event Action VictoryRetryRequested;

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
            HidePause();
            HideVictory();
            ShowGameplayHud();
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
            HidePause();
            HideVictory();
            ShowGameplayHud();
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
            ApplyCrosshairVisibility();
        }

        public void HideGameOver()
        {
            ResolveElements();
            IsGameOverVisible = false;
            if (_gameOverPanel != null)
            {
                _gameOverPanel.style.display = DisplayStyle.None;
            }

            ApplyCrosshairVisibility();
        }

        public void ShowPause()
        {
            ResolveElements();
            IsPauseVisible = true;
            if (_pausePanel != null)
            {
                _pausePanel.style.display = DisplayStyle.Flex;
            }

            ApplyCrosshairVisibility();
            _resumeButton?.Focus();
        }

        public void HidePause()
        {
            ResolveElements();
            IsPauseVisible = false;
            if (_pausePanel != null)
            {
                _pausePanel.style.display = DisplayStyle.None;
            }

            ApplyCrosshairVisibility();
        }

        public void ShowVictory()
        {
            ResolveElements();
            IsVictoryVisible = true;
            if (_victoryPanel != null)
            {
                _victoryPanel.style.display = DisplayStyle.Flex;
            }

            ApplyCrosshairVisibility();
        }

        public void HideVictory()
        {
            ResolveElements();
            IsVictoryVisible = false;
            if (_victoryPanel != null)
            {
                _victoryPanel.style.display = DisplayStyle.None;
            }

            ApplyCrosshairVisibility();
        }

        public void HideGameplayHud()
        {
            ResolveElements();
            _gameplayHudHidden = true;
            SetGameplayElementDisplay(DisplayStyle.None);
            ApplyCrosshairVisibility();
        }

        public void ShowGameplayHud()
        {
            ResolveElements();
            _gameplayHudHidden = false;
            SetGameplayElementDisplay(DisplayStyle.Flex);
            ApplyCrosshairVisibility();
        }

        public void RequestRetry()
        {
            RetryRequested?.Invoke();
        }

        public void RequestResume()
        {
            ResumeRequested?.Invoke();
        }

        public void RequestPauseRetry()
        {
            PauseRetryRequested?.Invoke();
        }

        public void RequestVictoryRetry()
        {
            VictoryRetryRequested?.Invoke();
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
            _pausePanel = root.Q<VisualElement>("pause-panel");
            _resumeButton = root.Q<Button>("resume-button");
            _pauseRetryButton = root.Q<Button>("pause-retry-button");
            _victoryPanel = root.Q<VisualElement>("victory-panel");
            _victoryRetryButton = root.Q<Button>("victory-retry-button");
            _crosshair = root.Q<VisualElement>(className: "crosshair");
            _objectiveCard = root.Q<VisualElement>("objective-card");
            _portalCoreCard = root.Q<VisualElement>("portal-core-card");
            _tutorialTip = root.Q<VisualElement>("tutorial-tip");
            _healthPanel = root.Q<VisualElement>(className: "player-health-panel");
            _interactionPrompt = root.Q<VisualElement>("interaction-prompt");
            _timedInteractionPanel = root.Q<VisualElement>("timed-interaction-panel");
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
            _resumeButton?.RegisterCallback<ClickEvent>(HandleResumeClicked);
            _pauseRetryButton?.RegisterCallback<ClickEvent>(HandlePauseRetryClicked);
            _victoryRetryButton?.RegisterCallback<ClickEvent>(HandleVictoryRetryClicked);
            _buttonBound = true;
        }

        private void UnbindRetryButton()
        {
            if (_buttonBound && _retryButton != null)
            {
                _retryButton.clicked -= RequestRetry;
            }

            _resumeButton?.UnregisterCallback<ClickEvent>(HandleResumeClicked);
            _pauseRetryButton?.UnregisterCallback<ClickEvent>(HandlePauseRetryClicked);
            _victoryRetryButton?.UnregisterCallback<ClickEvent>(HandleVictoryRetryClicked);

            _buttonBound = false;
        }

        private void HandleResumeClicked(ClickEvent clickEvent)
        {
            RequestResume();
        }

        private void HandlePauseRetryClicked(ClickEvent clickEvent)
        {
            RequestPauseRetry();
        }

        private void HandleVictoryRetryClicked(ClickEvent clickEvent)
        {
            RequestVictoryRetry();
        }

        private void ApplyCrosshairVisibility()
        {
            if (_crosshair != null)
            {
                bool visible = !_gameplayHudHidden && !IsGameOverVisible &&
                               !IsPauseVisible && !IsVictoryVisible;
                _crosshair.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void SetGameplayElementDisplay(DisplayStyle displayStyle)
        {
            if (_objectiveCard != null)
            {
                _objectiveCard.style.display = displayStyle;
            }

            if (_portalCoreCard != null)
            {
                _portalCoreCard.style.display = displayStyle;
            }

            if (_healthPanel != null)
            {
                _healthPanel.style.display = displayStyle;
            }

            if (_interactionPrompt != null)
            {
                _interactionPrompt.style.display = displayStyle;
            }

            if (_timedInteractionPanel != null && displayStyle == DisplayStyle.None)
            {
                _timedInteractionPanel.style.display = DisplayStyle.None;
            }

            if (_tutorialTip != null && displayStyle == DisplayStyle.None)
            {
                _tutorialTip.style.display = DisplayStyle.None;
            }
        }
    }
}
