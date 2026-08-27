using UnityEngine;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _styleSheet;

        private Label _promptLabel;
        private Label _statusLabel;
        private VisualElement _timedPanel;
        private Label _timedActionLabel;
        private ProgressBar _timedProgress;
        private VisualElement _styledRoot;

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
            SetPrompt(string.Empty);
            HideTimedProgress();
        }

        public void Configure(UIDocument document, StyleSheet styleSheet)
        {
            _document = document;
            _styleSheet = styleSheet;
            ResolveElements();
        }

        public void SetPrompt(string prompt)
        {
            if (_promptLabel == null)
            {
                ResolveElements();
            }

            if (_promptLabel == null)
            {
                return;
            }

            _promptLabel.text = prompt;
            _promptLabel.style.display = string.IsNullOrWhiteSpace(prompt)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        public void SetStatus(string status)
        {
            if (_statusLabel == null)
            {
                ResolveElements();
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = status;
            }
        }

        public void ShowTimedProgress(string actionLabel, float normalizedProgress)
        {
            if (_timedPanel == null || _timedActionLabel == null || _timedProgress == null)
            {
                ResolveElements();
            }

            if (_timedPanel == null || _timedActionLabel == null || _timedProgress == null)
            {
                return;
            }

            _timedActionLabel.text = actionLabel;
            _timedProgress.value = Mathf.Clamp01(normalizedProgress) * 100f;
            _timedPanel.style.display = DisplayStyle.Flex;
        }

        public void HideTimedProgress()
        {
            if (_timedPanel == null)
            {
                ResolveElements();
            }

            if (_timedPanel != null)
            {
                _timedPanel.style.display = DisplayStyle.None;
            }

            if (_timedProgress != null)
            {
                _timedProgress.value = 0f;
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

            _promptLabel = root.Q<Label>("interaction-prompt");
            _statusLabel = root.Q<Label>("prototype-status");
            _timedPanel = root.Q<VisualElement>("timed-interaction-panel");
            _timedActionLabel = root.Q<Label>("timed-interaction-label");
            _timedProgress = root.Q<ProgressBar>("timed-interaction-progress");
        }
    }
}
