using System;
using PlatformerUltra.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerUltra.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FactoryGameOverController : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private PlayerStatusPresenter _statusPresenter;
        [SerializeField] private ThirdPersonPlayerController _playerController;
        [SerializeField] private PlayerInteractor _playerInteractor;
        [SerializeField] private ThirdPersonOrbitCamera _orbitCamera;
        [SerializeField] private Renderer[] _playerRenderers = Array.Empty<Renderer>();

        private bool _bound;

        public bool IsGameOver { get; private set; }

        public event Action RetryRequested;

        private void OnEnable()
        {
            RefreshPlayerRenderers();
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Configure(
            PlayerHealth playerHealth,
            PlayerStatusPresenter statusPresenter,
            ThirdPersonPlayerController playerController,
            PlayerInteractor playerInteractor,
            ThirdPersonOrbitCamera orbitCamera,
            Renderer[] playerRenderers)
        {
            Unbind();
            _playerHealth = playerHealth;
            _statusPresenter = statusPresenter;
            _playerController = playerController;
            _playerInteractor = playerInteractor;
            _orbitCamera = orbitCamera;
            _playerRenderers = playerRenderers ?? Array.Empty<Renderer>();
            RefreshPlayerRenderers();
            Bind();
        }

        private void HandlePlayerDied(DamageInfo damageInfo)
        {
            EnterGameOver();
        }

        public void EnterGameOver()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            RefreshPlayerRenderers();
            if (_playerInteractor != null)
            {
                _playerInteractor.enabled = false;
            }

            _playerController?.SetLocomotionLocked(true);
            if (_orbitCamera != null)
            {
                _orbitCamera.enabled = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            for (int index = 0; index < _playerRenderers.Length; index++)
            {
                if (_playerRenderers[index] != null)
                {
                    _playerRenderers[index].enabled = false;
                }
            }

            _statusPresenter?.ShowGameOver();
        }

        private void HandleRetryRequested()
        {
            RetryRequested?.Invoke();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (!Application.isPlaying)
            {
                return;
            }

            FactoryRunSceneLoader.LoadNewRun();
        }

        private void Bind()
        {
            if (_bound || !isActiveAndEnabled)
            {
                return;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Died += HandlePlayerDied;
            }

            if (_statusPresenter != null)
            {
                _statusPresenter.RetryRequested += HandleRetryRequested;
            }

            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            if (_playerHealth != null)
            {
                _playerHealth.Died -= HandlePlayerDied;
            }

            if (_statusPresenter != null)
            {
                _statusPresenter.RetryRequested -= HandleRetryRequested;
            }

            _bound = false;
        }

        private void RefreshPlayerRenderers()
        {
            if (_playerController != null)
            {
                _playerRenderers = _playerController.GetComponentsInChildren<Renderer>(true);
            }
        }
    }
}
