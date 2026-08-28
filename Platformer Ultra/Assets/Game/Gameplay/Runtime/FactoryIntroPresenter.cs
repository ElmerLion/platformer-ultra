using UnityEngine;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FactoryIntroPresenter : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _styleSheet;

        private VisualElement _bootVeil;
        private Label _bootStatus;
        private ProgressBar _bootProgress;
        private VisualElement _coreRequirement;
        private VisualElement _subtitlePanel;
        private Label _subtitleSpeaker;
        private Label _subtitleText;
        private VisualElement _skipPanel;
        private ProgressBar _skipProgress;
        private VisualElement _transitionOverlay;
        private VisualElement _staticBandA;
        private VisualElement _staticBandB;
        private Label _syncLabel;
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
            ResetPresentation();
        }

        public void Configure(UIDocument document, StyleSheet styleSheet)
        {
            _document = document;
            _styleSheet = styleSheet;
            ResolveElements();
            ResetPresentation();
        }

        public void SetIntroTime(double time)
        {
            ResolveElements();
            float seconds = Mathf.Max(0f, (float)time);
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 2.25f, seconds));
            if (_bootVeil != null)
            {
                _bootVeil.style.display = reveal >= 0.999f ? DisplayStyle.None : DisplayStyle.Flex;
                _bootVeil.style.opacity = 1f - reveal;
            }

            if (_bootProgress != null)
            {
                _bootProgress.value = Mathf.Clamp01(seconds / 1.9f) * 100f;
            }

            if (_bootStatus != null)
            {
                _bootStatus.text = seconds < 1.05f
                    ? "MAINTENANCE UNIT // INITIALIZING"
                    : "CHASSIS LINK // RESTORED";
            }

            if (_coreRequirement != null)
            {
                _coreRequirement.style.display = seconds >= 14f && seconds < 19.15f
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        public void ShowSubtitle(string speaker, string subtitle)
        {
            ResolveElements();
            if (_subtitlePanel == null || _subtitleText == null)
            {
                return;
            }

            _subtitleSpeaker.text = string.IsNullOrWhiteSpace(speaker)
                ? "FACTORY EMERGENCY SYSTEM"
                : speaker;
            _subtitleText.text = subtitle ?? string.Empty;
            _subtitlePanel.style.display = string.IsNullOrWhiteSpace(subtitle)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        public void HideSubtitle()
        {
            if (_subtitlePanel != null)
            {
                _subtitlePanel.style.display = DisplayStyle.None;
            }
        }

        public void SetSkipProgress(bool visible, float normalized)
        {
            ResolveElements();
            if (_skipPanel != null)
            {
                _skipPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_skipProgress != null)
            {
                _skipProgress.value = Mathf.Clamp01(normalized) * 100f;
            }
        }

        public void BeginTransition()
        {
            ResolveElements();
            HideSubtitle();
            SetSkipProgress(false, 0f);
            if (_coreRequirement != null)
            {
                _coreRequirement.style.display = DisplayStyle.None;
            }

            if (_transitionOverlay != null)
            {
                _transitionOverlay.style.display = DisplayStyle.Flex;
                _transitionOverlay.style.opacity = 0f;
            }
        }

        public void SetTransition(float opacity, bool showSynchronizing, float glitchPhase)
        {
            ResolveElements();
            if (_transitionOverlay == null)
            {
                return;
            }

            _transitionOverlay.style.display = DisplayStyle.Flex;
            _transitionOverlay.style.opacity = Mathf.Clamp01(opacity);
            if (_syncLabel != null)
            {
                _syncLabel.style.display = showSynchronizing ? DisplayStyle.Flex : DisplayStyle.None;
            }

            float phase = Mathf.Repeat(glitchPhase, 1f);
            if (_staticBandA != null)
            {
                _staticBandA.style.top = Length.Percent(18f + phase * 66f);
                _staticBandA.style.opacity = 0.18f + Mathf.Abs(Mathf.Sin(glitchPhase * 17f)) * 0.48f;
            }

            if (_staticBandB != null)
            {
                _staticBandB.style.top = Length.Percent(82f - phase * 57f);
                _staticBandB.style.opacity = 0.12f + Mathf.Abs(Mathf.Cos(glitchPhase * 23f)) * 0.38f;
            }
        }

        public void HideTransition()
        {
            if (_transitionOverlay != null)
            {
                _transitionOverlay.style.display = DisplayStyle.None;
                _transitionOverlay.style.opacity = 0f;
            }
        }

        private void ResetPresentation()
        {
            SetIntroTime(0d);
            HideSubtitle();
            SetSkipProgress(false, 0f);
            HideTransition();
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

            _bootVeil = root.Q<VisualElement>("boot-veil");
            _bootStatus = root.Q<Label>("boot-status");
            _bootProgress = root.Q<ProgressBar>("boot-progress");
            _coreRequirement = root.Q<VisualElement>("core-requirement");
            _subtitlePanel = root.Q<VisualElement>("subtitle-panel");
            _subtitleSpeaker = root.Q<Label>("subtitle-speaker");
            _subtitleText = root.Q<Label>("subtitle-text");
            _skipPanel = root.Q<VisualElement>("skip-panel");
            _skipProgress = root.Q<ProgressBar>("skip-progress");
            _transitionOverlay = root.Q<VisualElement>("transition-overlay");
            _staticBandA = root.Q<VisualElement>("static-band-a");
            _staticBandB = root.Q<VisualElement>("static-band-b");
            _syncLabel = root.Q<Label>("sync-label");
        }
    }
}
