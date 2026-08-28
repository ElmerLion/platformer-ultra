using System;
using PlatformerUltra.Factory.Conveyors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlatformerUltra.Gameplay
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonPlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private ConveyorPassenger _conveyorPassenger;
        [SerializeField] private PlayerMovementSettings _settings;

        [Header("Input")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _sprintAction;
        [SerializeField] private InputActionReference _dashAction;

        [Header("Grounding")]
        [SerializeField, Min(0f)] private float _animationGroundedGraceTime = 0.12f;

        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _animationGroundedGraceTimer;
        private float _dashRemaining;
        private float _dashCooldownRemaining;
        private float _dashBufferTimer;
        private Vector3 _dashDirection;
        private int _airJumpsUsed;
        private bool _doubleJumpUnlocked;

        public bool IsGrounded { get; private set; }
        public bool IsAnimationGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsDashing => _dashRemaining > 0f;
        public bool DoubleJumpUnlocked => _doubleJumpUnlocked;
        public bool LocomotionLocked { get; private set; }
        public Vector3 Velocity => _planarVelocity + Vector3.up * _verticalVelocity;
        public float DashCooldownRemaining => _dashCooldownRemaining;
        public float AnimationPlanarSpeed => IsDashing && _settings != null
            ? _settings.SprintSpeed
            : _planarVelocity.magnitude;

        public event Action<bool> Jumped;
        public event Action<Vector3, bool> Dashed;

        private void Awake()
        {
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_conveyorPassenger == null)
            {
                _conveyorPassenger = GetComponent<ConveyorPassenger>();
            }

            if (_settings != null)
            {
                _doubleJumpUnlocked = _settings.DoubleJumpUnlockedForTesting;
            }

            IsGrounded = _characterController != null && _characterController.isGrounded;
            IsAnimationGrounded = IsGrounded;
            _animationGroundedGraceTimer = IsGrounded ? _animationGroundedGraceTime : 0f;
        }

        private void OnEnable()
        {
            _moveAction?.action.Enable();
            _jumpAction?.action.Enable();
            _sprintAction?.action.Enable();
            _dashAction?.action.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.action.Disable();
            _jumpAction?.action.Disable();
            _sprintAction?.action.Disable();
            _dashAction?.action.Disable();
            EndDash(false);
            IsSprinting = false;
        }

        private void Update()
        {
            if (_settings == null || _cameraTransform == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            IsGrounded = _characterController.isGrounded;
            if (LocomotionLocked)
            {
                UpdateLockedMovement(deltaTime);
                return;
            }

            UpdateJumpTimers(deltaTime);
            UpdateVerticalVelocity(deltaTime);
            UpdateDashTimers(deltaTime);
            TryConsumeBufferedDash();

            Vector3 conveyorVelocity = _conveyorPassenger != null
                ? _conveyorPassenger.CurrentSurfaceVelocity
                : Vector3.zero;
            if (IsDashing)
            {
                UpdateDashMovement(deltaTime, conveyorVelocity);
                return;
            }

            UpdateHorizontalVelocity(deltaTime);
            Vector3 movement = _planarVelocity + conveyorVelocity + Vector3.up * _verticalVelocity;
            CollisionFlags collisionFlags = _characterController.Move(movement * deltaTime);
            UpdateGroundedStateAfterMove(collisionFlags, deltaTime);
            RotateTowardsMovement(deltaTime);
        }

        public void Configure(
            CharacterController characterController,
            Transform cameraTransform,
            ConveyorPassenger conveyorPassenger,
            PlayerMovementSettings settings,
            InputActionReference moveAction,
            InputActionReference jumpAction,
            InputActionReference sprintAction,
            InputActionReference dashAction)
        {
            _characterController = characterController;
            _cameraTransform = cameraTransform;
            _conveyorPassenger = conveyorPassenger;
            _settings = settings;
            _moveAction = moveAction;
            _jumpAction = jumpAction;
            _sprintAction = sprintAction;
            _dashAction = dashAction;
            _doubleJumpUnlocked = settings != null && settings.DoubleJumpUnlockedForTesting;
        }

        public void UnlockDoubleJump()
        {
            _doubleJumpUnlocked = true;
        }

        public void SetDoubleJumpUnlocked(bool unlocked)
        {
            _doubleJumpUnlocked = unlocked;
            if (!unlocked)
            {
                _airJumpsUsed = 0;
            }
        }

        public void SetLocomotionLocked(bool locked)
        {
            LocomotionLocked = locked;
            if (!locked)
            {
                return;
            }

            _planarVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _dashBufferTimer = 0f;
            EndDash(false);
            IsSprinting = false;
        }

        public bool TryStartDash(Vector3 direction)
        {
            if (_settings == null || LocomotionLocked || IsDashing || _dashCooldownRemaining > 0f)
            {
                return false;
            }

            Vector3 planarDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (planarDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            BeginDash(planarDirection.normalized);
            return true;
        }

        private void UpdateLockedMovement(float deltaTime)
        {
            _planarVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            IsSprinting = false;

            if (IsGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _settings.Gravity * deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, _settings.TerminalVelocity);
            CollisionFlags collisionFlags = _characterController.Move(Vector3.up * _verticalVelocity * deltaTime);
            UpdateGroundedStateAfterMove(collisionFlags, deltaTime);
        }

        private void UpdateJumpTimers(float deltaTime)
        {
            if (IsGrounded)
            {
                _coyoteTimer = _settings.CoyoteTime;
                _airJumpsUsed = 0;
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }
            }
            else
            {
                _coyoteTimer -= deltaTime;
            }

            if (_jumpAction != null && _jumpAction.action.WasPressedThisFrame())
            {
                _jumpBufferTimer = _settings.JumpBufferTime;
            }
            else
            {
                _jumpBufferTimer -= deltaTime;
            }
        }

        private void UpdateDashTimers(float deltaTime)
        {
            _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - deltaTime);
            if (_dashAction != null && _dashAction.action.WasPressedThisFrame())
            {
                _dashBufferTimer = _settings.DashInputBufferTime;
            }
            else
            {
                _dashBufferTimer = Mathf.Max(0f, _dashBufferTimer - deltaTime);
            }
        }

        private void TryConsumeBufferedDash()
        {
            if (_dashBufferTimer <= 0f || IsDashing || _dashCooldownRemaining > 0f)
            {
                return;
            }

            Vector3 direction = ResolveDashDirection(
                ReadMoveInput(),
                _cameraTransform.forward,
                _cameraTransform.right,
                _planarVelocity,
                transform.forward);
            if (TryStartDash(direction))
            {
                _dashBufferTimer = 0f;
            }
        }

        private void BeginDash(Vector3 direction)
        {
            bool airborne = !IsGrounded;
            _dashDirection = direction;
            _dashRemaining = _settings.DashDuration;
            _dashCooldownRemaining = _settings.DashCooldown;
            _planarVelocity = direction * _settings.DashSpeed;
            IsSprinting = false;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            Dashed?.Invoke(direction, airborne);
        }

        private void UpdateDashMovement(float deltaTime, Vector3 conveyorVelocity)
        {
            float dashStep = Mathf.Min(deltaTime, _dashRemaining);
            _dashRemaining = Mathf.Max(0f, _dashRemaining - deltaTime);
            _planarVelocity = _dashDirection * _settings.DashSpeed;
            Vector3 movement = _planarVelocity * dashStep
                + (conveyorVelocity + Vector3.up * _verticalVelocity) * deltaTime;
            CollisionFlags collisionFlags = _characterController.Move(movement);
            UpdateGroundedStateAfterMove(collisionFlags, deltaTime);
            transform.rotation = Quaternion.LookRotation(_dashDirection, Vector3.up);

            if ((collisionFlags & CollisionFlags.Sides) != 0 || _dashRemaining <= 0f)
            {
                EndDash(true);
            }
        }

        private void EndDash(bool preserveExitSpeed)
        {
            if (_dashRemaining <= 0f && _dashDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _dashRemaining = 0f;
            if (preserveExitSpeed && _settings != null)
            {
                _planarVelocity = _dashDirection * _settings.DashExitSpeed;
            }
            else if (!preserveExitSpeed)
            {
                _planarVelocity = Vector3.zero;
            }

            _dashDirection = Vector3.zero;
        }

        private void UpdateHorizontalVelocity(float deltaTime)
        {
            Vector2 input = ReadMoveInput();

            Vector3 cameraForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized;
            Vector3 desiredDirection = (cameraForward * input.y + cameraRight * input.x).normalized;
            IsSprinting = input.sqrMagnitude > 0.0001f;
            float targetSpeed = _settings.MovementSpeed;
            Vector3 targetVelocity = desiredDirection * targetSpeed * input.magnitude;

            float acceleration;
            if (!IsGrounded)
            {
                acceleration = _settings.AirAcceleration;
            }
            else if (targetVelocity.sqrMagnitude > 0.0001f)
            {
                acceleration = _settings.GroundAcceleration;
            }
            else
            {
                acceleration = _settings.GroundDeceleration;
            }

            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, acceleration * deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 input = _moveAction != null
                ? _moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;
            return Vector2.ClampMagnitude(input, 1f);
        }

        public static Vector3 ResolveDashDirection(
            Vector2 movementInput,
            Vector3 cameraForward,
            Vector3 cameraRight,
            Vector3 currentPlanarVelocity,
            Vector3 facingForward)
        {
            Vector2 input = Vector2.ClampMagnitude(movementInput, 1f);
            Vector3 forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraRight, Vector3.up).normalized;
            Vector3 inputDirection = forward * input.y + right * input.x;
            if (inputDirection.sqrMagnitude > 0.0001f)
            {
                return inputDirection.normalized;
            }

            Vector3 velocityDirection = Vector3.ProjectOnPlane(currentPlanarVelocity, Vector3.up);
            if (velocityDirection.sqrMagnitude > 0.0001f)
            {
                return velocityDirection.normalized;
            }

            Vector3 fallback = Vector3.ProjectOnPlane(facingForward, Vector3.up);
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        private void UpdateVerticalVelocity(float deltaTime)
        {
            if (_jumpBufferTimer > 0f)
            {
                bool canGroundJump = IsGrounded || _coyoteTimer > 0f;
                bool canAirJump = !canGroundJump && _doubleJumpUnlocked && _airJumpsUsed == 0;
                if (canGroundJump || canAirJump)
                {
                    _verticalVelocity = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
                    _jumpBufferTimer = 0f;
                    _coyoteTimer = 0f;
                    if (canAirJump)
                    {
                        _airJumpsUsed++;
                    }

                    Jumped?.Invoke(canAirJump);
                }
            }

            _verticalVelocity += _settings.Gravity * deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, _settings.TerminalVelocity);
        }

        private void UpdateGroundedStateAfterMove(CollisionFlags collisionFlags, float deltaTime)
        {
            bool groundedAfterMove = (collisionFlags & CollisionFlags.Below) != 0 ||
                                     _characterController.isGrounded;
            bool hitCeiling = (collisionFlags & CollisionFlags.Above) != 0;

            if (hitCeiling && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }

            IsGrounded = groundedAfterMove;
            if (groundedAfterMove)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }

                _animationGroundedGraceTimer = _animationGroundedGraceTime;
                IsAnimationGrounded = true;
                return;
            }

            if (_verticalVelocity > 0.05f)
            {
                _animationGroundedGraceTimer = 0f;
                IsAnimationGrounded = false;
                return;
            }

            _animationGroundedGraceTimer = Mathf.Max(0f, _animationGroundedGraceTimer - deltaTime);
            IsAnimationGrounded = _animationGroundedGraceTimer > 0f;
        }

        private void RotateTowardsMovement(float deltaTime)
        {
            Vector3 direction = Vector3.ProjectOnPlane(_planarVelocity, Vector3.up);
            if (direction.sqrMagnitude <= 0.01f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float interpolation = 1f - Mathf.Exp(-_settings.RotationSharpness * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, interpolation);
        }
    }
}
