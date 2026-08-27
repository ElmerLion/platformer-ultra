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

        [Header("Grounding")]
        [SerializeField, Min(0f)] private float _animationGroundedGraceTime = 0.12f;

        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _animationGroundedGraceTimer;
        private int _airJumpsUsed;
        private bool _doubleJumpUnlocked;

        public bool IsGrounded { get; private set; }
        public bool IsAnimationGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool DoubleJumpUnlocked => _doubleJumpUnlocked;
        public bool LocomotionLocked { get; private set; }
        public Vector3 Velocity => _planarVelocity + Vector3.up * _verticalVelocity;

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
        }

        private void OnDisable()
        {
            _moveAction?.action.Disable();
            _jumpAction?.action.Disable();
            _sprintAction?.action.Disable();
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
            UpdateHorizontalVelocity(deltaTime);
            UpdateVerticalVelocity(deltaTime);

            Vector3 conveyorVelocity = _conveyorPassenger != null
                ? _conveyorPassenger.CurrentSurfaceVelocity
                : Vector3.zero;
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
            InputActionReference sprintAction)
        {
            _characterController = characterController;
            _cameraTransform = cameraTransform;
            _conveyorPassenger = conveyorPassenger;
            _settings = settings;
            _moveAction = moveAction;
            _jumpAction = jumpAction;
            _sprintAction = sprintAction;
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
            IsSprinting = false;
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

        private void UpdateHorizontalVelocity(float deltaTime)
        {
            Vector2 input = _moveAction != null
                ? _moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 cameraForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized;
            Vector3 desiredDirection = (cameraForward * input.y + cameraRight * input.x).normalized;
            bool sprintHeld = _sprintAction != null && _sprintAction.action.IsPressed();
            IsSprinting = sprintHeld && input.sqrMagnitude > 0.0001f;
            float targetSpeed = IsSprinting ? _settings.SprintSpeed : _settings.MovementSpeed;
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
