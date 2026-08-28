using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryIntroController : MonoBehaviour
    {
        [Serializable]
        public struct VoiceCue
        {
            [Min(0f)] public float StartTime;
            [Min(0)] public int LineIndex;
        }

        [SerializeField] private PlayableDirector _director;
        [SerializeField] private FactoryAIVoiceEmitter _voiceEmitter;
        [SerializeField] private FactoryIntroPresenter _presenter;
        [SerializeField] private FactorySceneTransition _transition;
        [SerializeField] private InputActionReference _skipAction;
        [SerializeField] private VoiceCue[] _voiceCues = Array.Empty<VoiceCue>();
        [SerializeField, Min(1f)] private float _sequenceDuration = 19.15f;
        [SerializeField, Min(0f)] private float _skipAvailableAt = 1f;
        [SerializeField, Min(0.1f)] private float _skipHoldDuration = 0.6f;
        [SerializeField] private string _factorySceneName = FactoryRunSceneLoader.FactorySceneName;

        private AsyncOperation _factoryLoad;
        private bool[] _playedCues = Array.Empty<bool>();
        private float _skipHeldFor;
        private bool _transitionStarted;
        private bool _inputHandedOff;

        public bool TransitionStarted => _transitionStarted;
        public AsyncOperation FactoryLoad => _factoryLoad;

        private void OnEnable()
        {
            _skipAction?.action.Enable();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _playedCues = new bool[_voiceCues != null ? _voiceCues.Length : 0];
            BeginFactoryPreload();
            if (_director != null)
            {
                _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                _director.time = 0d;
                _director.Play();
            }
        }

        private void Update()
        {
            if (_transitionStarted)
            {
                return;
            }

            double time = _director != null ? _director.time : Time.unscaledTimeAsDouble;
            _presenter?.SetIntroTime(time);
            PlayDueVoiceCues(time);
            UpdateSkip((float)time);

            if (time >= _sequenceDuration ||
                (_director != null && _director.state != PlayState.Playing && time > 1d))
            {
                CompleteIntro();
            }
        }

        private void OnDisable()
        {
            if (!_inputHandedOff)
            {
                _skipAction?.action.Disable();
            }
        }

        public void Configure(
            PlayableDirector director,
            FactoryAIVoiceEmitter voiceEmitter,
            FactoryIntroPresenter presenter,
            FactorySceneTransition transition,
            InputActionReference skipAction,
            VoiceCue[] voiceCues,
            float sequenceDuration,
            string factorySceneName = FactoryRunSceneLoader.FactorySceneName)
        {
            _director = director;
            _voiceEmitter = voiceEmitter;
            _presenter = presenter;
            _transition = transition;
            _skipAction = skipAction;
            _voiceCues = voiceCues ?? Array.Empty<VoiceCue>();
            _sequenceDuration = Mathf.Max(1f, sequenceDuration);
            _factorySceneName = string.IsNullOrWhiteSpace(factorySceneName)
                ? FactoryRunSceneLoader.FactorySceneName
                : factorySceneName;
        }

        public void RequestSkip()
        {
            BeginTransition();
        }

        public void CompleteIntro()
        {
            BeginTransition();
        }

        private void BeginFactoryPreload()
        {
            try
            {
                _factoryLoad = SceneManager.LoadSceneAsync(_factorySceneName, LoadSceneMode.Single);
                if (_factoryLoad != null)
                {
                    _factoryLoad.allowSceneActivation = false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not preload factory scene '{_factorySceneName}': {exception.Message}", this);
                _factoryLoad = null;
            }
        }

        private void PlayDueVoiceCues(double time)
        {
            if (_voiceCues == null)
            {
                return;
            }

            for (int index = 0; index < _voiceCues.Length; index++)
            {
                if (_playedCues[index] || time < _voiceCues[index].StartTime)
                {
                    continue;
                }

                _playedCues[index] = true;
                _voiceEmitter?.PlayLine(_voiceCues[index].LineIndex);
            }
        }

        private void UpdateSkip(float time)
        {
            bool available = time >= _skipAvailableAt;
            bool pressed = available && _skipAction != null && _skipAction.action.IsPressed();
            _skipHeldFor = pressed
                ? _skipHeldFor + Time.unscaledDeltaTime
                : 0f;
            float progress = _skipHeldFor / Mathf.Max(0.1f, _skipHoldDuration);
            _presenter?.SetSkipProgress(available, progress);
            if (progress >= 1f)
            {
                RequestSkip();
            }
        }

        private void BeginTransition()
        {
            if (_transitionStarted)
            {
                return;
            }

            _transitionStarted = true;
            _director?.Stop();
            _voiceEmitter?.StopAll();
            if (_transition == null)
            {
                Debug.LogError("Factory intro has no scene transition controller; activating the factory directly.", this);
                if (_factoryLoad != null)
                {
                    _factoryLoad.allowSceneActivation = true;
                }
                else
                {
                    SceneManager.LoadSceneAsync(_factorySceneName, LoadSceneMode.Single);
                }
                return;
            }

            _inputHandedOff = true;
            _transition.BeginTransition(_factoryLoad, _skipAction, _factorySceneName);
        }
    }
}
