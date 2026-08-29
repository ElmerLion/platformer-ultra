using PlatformerUltra.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FactorySettingsPresenter : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private AudioSettingsController _audioSettings;

        private VisualElement _pauseCard;
        private VisualElement _settingsCard;
        private Button _optionsButton;
        private Button _backButton;
        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private Label _masterValue;
        private Label _musicValue;
        private Label _sfxValue;
        private bool _bound;

        public bool IsOpen { get; private set; }
        public float MasterSliderValue => _masterSlider?.value ?? 100f;
        public float MusicSliderValue => _musicSlider?.value ?? 100f;
        public float SfxSliderValue => _sfxSlider?.value ?? 100f;

        private void Awake()
        {
            _document ??= GetComponent<UIDocument>();
            ResolveElements();
        }

        private void OnEnable()
        {
            ResolveElements();
            Bind();
            RefreshValues();
            ShowPauseMenu(false);
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Configure(UIDocument document, AudioSettingsController audioSettings)
        {
            Unbind();
            _document = document;
            _audioSettings = audioSettings;
            ResolveElements();
            Bind();
            RefreshValues();
            ShowPauseMenu(false);
        }

        public void ShowOptions()
        {
            ResolveElements();
            IsOpen = true;
            SetDisplay(_pauseCard, DisplayStyle.None);
            SetDisplay(_settingsCard, DisplayStyle.Flex);
            _masterSlider?.Focus();
        }

        public void ShowPauseMenu(bool focusOptions = true)
        {
            ResolveElements();
            IsOpen = false;
            SetDisplay(_settingsCard, DisplayStyle.None);
            SetDisplay(_pauseCard, DisplayStyle.Flex);
            if (focusOptions)
            {
                _optionsButton?.Focus();
            }
        }

        public void RefreshValues()
        {
            if (_audioSettings == null)
            {
                return;
            }

            SetSliderWithoutNotify(_masterSlider, _audioSettings.MasterVolume * 100f);
            SetSliderWithoutNotify(_musicSlider, _audioSettings.MusicVolume * 100f);
            SetSliderWithoutNotify(_sfxSlider, _audioSettings.SfxVolume * 100f);
            UpdateValueLabel(_masterValue, _audioSettings.MasterVolume);
            UpdateValueLabel(_musicValue, _audioSettings.MusicVolume);
            UpdateValueLabel(_sfxValue, _audioSettings.SfxVolume);
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null)
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            _pauseCard = root.Q<VisualElement>("pause-card");
            _settingsCard = root.Q<VisualElement>("settings-card");
            _optionsButton = root.Q<Button>("options-button");
            _backButton = root.Q<Button>("settings-back-button");
            _masterSlider = root.Q<Slider>("master-volume-slider");
            _musicSlider = root.Q<Slider>("music-volume-slider");
            _sfxSlider = root.Q<Slider>("sfx-volume-slider");
            _masterValue = root.Q<Label>("master-volume-value");
            _musicValue = root.Q<Label>("music-volume-value");
            _sfxValue = root.Q<Label>("sfx-volume-value");
        }

        private void Bind()
        {
            if (_bound || !isActiveAndEnabled)
            {
                return;
            }

            _optionsButton?.RegisterCallback<ClickEvent>(HandleOptionsClicked);
            _backButton?.RegisterCallback<ClickEvent>(HandleBackClicked);
            _masterSlider?.RegisterValueChangedCallback(HandleMasterChanged);
            _musicSlider?.RegisterValueChangedCallback(HandleMusicChanged);
            _sfxSlider?.RegisterValueChangedCallback(HandleSfxChanged);
            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            _optionsButton?.UnregisterCallback<ClickEvent>(HandleOptionsClicked);
            _backButton?.UnregisterCallback<ClickEvent>(HandleBackClicked);
            _masterSlider?.UnregisterValueChangedCallback(HandleMasterChanged);
            _musicSlider?.UnregisterValueChangedCallback(HandleMusicChanged);
            _sfxSlider?.UnregisterValueChangedCallback(HandleSfxChanged);
            _bound = false;
        }

        private void HandleOptionsClicked(ClickEvent clickEvent)
        {
            ShowOptions();
        }

        private void HandleBackClicked(ClickEvent clickEvent)
        {
            ShowPauseMenu();
        }

        private void HandleMasterChanged(ChangeEvent<float> changeEvent)
        {
            float normalized = Mathf.Clamp01(changeEvent.newValue / 100f);
            _audioSettings?.SetMasterVolume(normalized);
            UpdateValueLabel(_masterValue, normalized);
        }

        private void HandleMusicChanged(ChangeEvent<float> changeEvent)
        {
            float normalized = Mathf.Clamp01(changeEvent.newValue / 100f);
            _audioSettings?.SetMusicVolume(normalized);
            UpdateValueLabel(_musicValue, normalized);
        }

        private void HandleSfxChanged(ChangeEvent<float> changeEvent)
        {
            float normalized = Mathf.Clamp01(changeEvent.newValue / 100f);
            _audioSettings?.SetSfxVolume(normalized);
            UpdateValueLabel(_sfxValue, normalized);
        }

        private static void SetDisplay(VisualElement element, DisplayStyle displayStyle)
        {
            if (element != null)
            {
                element.style.display = displayStyle;
            }
        }

        private static void SetSliderWithoutNotify(Slider slider, float value)
        {
            slider?.SetValueWithoutNotify(Mathf.Clamp(value, 0f, 100f));
        }

        private static void UpdateValueLabel(Label label, float normalized)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f) + "%";
            }
        }
    }
}
