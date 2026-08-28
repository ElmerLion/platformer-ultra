using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactorySceneTransition : MonoBehaviour
    {
        [SerializeField] private FactoryIntroPresenter _presenter;
        [SerializeField] private AudioSource _transitionSource;
        [SerializeField] private AudioClip _transitionClip;
        [SerializeField, Min(0.05f)] private float _fadeInDuration = 0.24f;
        [SerializeField, Min(0.05f)] private float _fadeOutDuration = 0.35f;
        [SerializeField, Min(0f)] private float _syncLabelDelay = 0.45f;

        private static FactorySceneTransition _instance;
        private Coroutine _routine;

        public static bool IsTransitioningToFactory { get; private set; }
        public static FactorySceneTransition Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _instance = null;
            IsTransitioningToFactory = false;
        }

        public void Configure(
            FactoryIntroPresenter presenter,
            AudioSource transitionSource,
            AudioClip transitionClip)
        {
            _presenter = presenter;
            _transitionSource = transitionSource;
            _transitionClip = transitionClip;
        }

        public bool BeginTransition(
            AsyncOperation factoryLoad,
            InputActionReference skipAction,
            string factorySceneName)
        {
            if (_routine != null)
            {
                return false;
            }

            IsTransitioningToFactory = true;
            if (_transitionSource != null && _transitionClip != null)
            {
                _transitionSource.PlayOneShot(_transitionClip);
            }

            _routine = StartCoroutine(RunTransition(factoryLoad, skipAction, factorySceneName));
            return true;
        }

        private IEnumerator RunTransition(
            AsyncOperation factoryLoad,
            InputActionReference skipAction,
            string factorySceneName)
        {
            _presenter?.BeginTransition();
            float phase = 0f;
            for (float elapsed = 0f; elapsed < _fadeInDuration; elapsed += Time.unscaledDeltaTime)
            {
                phase += Time.unscaledDeltaTime * 1.7f;
                _presenter?.SetTransition(elapsed / _fadeInDuration, false, phase);
                yield return null;
            }

            _presenter?.SetTransition(1f, false, phase);
            if (factoryLoad == null)
            {
                try
                {
                    factoryLoad = SceneManager.LoadSceneAsync(factorySceneName, LoadSceneMode.Single);
                    if (factoryLoad != null)
                    {
                        factoryLoad.allowSceneActivation = false;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Could not load factory scene '{factorySceneName}': {exception.Message}", this);
                }
            }

            float waiting = 0f;
            while (factoryLoad != null && factoryLoad.progress < 0.9f)
            {
                waiting += Time.unscaledDeltaTime;
                phase += Time.unscaledDeltaTime * 1.7f;
                _presenter?.SetTransition(1f, waiting >= _syncLabelDelay, phase);
                yield return null;
            }

            if (factoryLoad != null)
            {
                factoryLoad.allowSceneActivation = true;
                while (!factoryLoad.isDone)
                {
                    phase += Time.unscaledDeltaTime * 1.7f;
                    _presenter?.SetTransition(1f, true, phase);
                    yield return null;
                }
            }

            yield return null;
            yield return new WaitForEndOfFrame();

            while (skipAction != null && skipAction.action.IsPressed())
            {
                phase += Time.unscaledDeltaTime * 1.7f;
                _presenter?.SetTransition(1f, false, phase);
                yield return null;
            }

            FactorySceneEntryController entry = FactorySceneEntryController.Current;
            entry?.ReleaseGameplay();
            yield return null;

            float transitionVolume = _transitionSource != null ? _transitionSource.volume : 0f;
            for (float elapsed = 0f; elapsed < _fadeOutDuration; elapsed += Time.unscaledDeltaTime)
            {
                float fade = Mathf.Clamp01(elapsed / _fadeOutDuration);
                phase += Time.unscaledDeltaTime * 1.7f;
                _presenter?.SetTransition(1f - fade, false, phase);
                if (_transitionSource != null)
                {
                    _transitionSource.volume = transitionVolume * (1f - fade);
                }
                yield return null;
            }

            _presenter?.HideTransition();
            IsTransitioningToFactory = false;
            _routine = null;
            Destroy(gameObject);
        }
    }
}
