using UnityEngine;

namespace PlatformerUltra.Gameplay
{
    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Platformer Ultra/Player Movement Settings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("Horizontal Movement")]
        [SerializeField, Min(0f)] private float _movementSpeed = 1.55f;
        [SerializeField, Min(0f)] private float _sprintSpeed = 3.525f;
        [SerializeField, Min(0f)] private float _groundAcceleration = 18f;
        [SerializeField, Min(0f)] private float _groundDeceleration = 24f;
        [SerializeField, Min(0f)] private float _airAcceleration = 7f;
        [SerializeField, Min(0f)] private float _rotationSharpness = 16f;

        [Header("Jumping")]
        [SerializeField, Min(0.1f)] private float _jumpHeight = 1.8f;
        [SerializeField] private float _gravity = -28f;
        [SerializeField] private float _terminalVelocity = -45f;
        [SerializeField, Min(0f)] private float _coyoteTime = 0.15f;
        [SerializeField, Min(0f)] private float _jumpBufferTime = 0.15f;
        [SerializeField] private bool _doubleJumpUnlockedForTesting;

        [Header("Dashing")]
        [SerializeField, Min(0.1f)] private float _dashDistance = 2.4f;
        [SerializeField, Min(0.05f)] private float _dashDuration = 0.2f;
        [SerializeField, Min(0f)] private float _dashCooldown = 1.5f;
        [SerializeField, Min(0f)] private float _dashInputBufferTime = 0.12f;
        [SerializeField, Min(0f)] private float _dashExitSpeed = 3.525f;

        public float MovementSpeed => _movementSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float GroundAcceleration => _groundAcceleration;
        public float GroundDeceleration => _groundDeceleration;
        public float AirAcceleration => _airAcceleration;
        public float RotationSharpness => _rotationSharpness;
        public float JumpHeight => _jumpHeight;
        public float Gravity => _gravity;
        public float TerminalVelocity => _terminalVelocity;
        public float CoyoteTime => _coyoteTime;
        public float JumpBufferTime => _jumpBufferTime;
        public bool DoubleJumpUnlockedForTesting => _doubleJumpUnlockedForTesting;
        public float DashDistance => _dashDistance;
        public float DashDuration => _dashDuration;
        public float DashCooldown => _dashCooldown;
        public float DashInputBufferTime => _dashInputBufferTime;
        public float DashExitSpeed => _dashExitSpeed;
        public float DashSpeed => _dashDistance / Mathf.Max(0.05f, _dashDuration);
    }
}
