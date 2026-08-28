using Unity.AI.Navigation;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshLink))]
    public sealed class EnemyTraversalLink : MonoBehaviour
    {
        [SerializeField] private NavMeshLink _link;
        [SerializeField] private EnemyTraversalKind _kind = EnemyTraversalKind.Jump;
        [SerializeField] private Vector3 _localFacingDirection = Vector3.forward;
        [SerializeField, Min(0f)] private float _jumpArcHeight = 0.8f;
        [SerializeField, Min(0.1f)] private float _jumpSpeed = 4.5f;
        [SerializeField, Min(0f)] private float _alignDuration = 0.18f;
        [SerializeField, Min(0f)] private float _dismountDuration = 0.18f;

        public NavMeshLink Link => _link;
        public EnemyTraversalKind Kind => _kind;
        public float JumpArcHeight => _jumpArcHeight;
        public float JumpSpeed => _jumpSpeed;
        public float AlignDuration => _alignDuration;
        public float DismountDuration => _dismountDuration;
        public Vector3 FacingDirectionWorld
        {
            get
            {
                Vector3 direction = transform.TransformDirection(_localFacingDirection);
                return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            }
        }

        private void Awake()
        {
            if (_link == null)
            {
                _link = GetComponent<NavMeshLink>();
            }
        }

        private void OnValidate()
        {
            if (_link == null)
            {
                _link = GetComponent<NavMeshLink>();
            }

            if (_localFacingDirection.sqrMagnitude <= 0.0001f)
            {
                _localFacingDirection = Vector3.forward;
            }

            _jumpArcHeight = Mathf.Max(0f, _jumpArcHeight);
            _jumpSpeed = Mathf.Max(0.1f, _jumpSpeed);
            _alignDuration = Mathf.Max(0f, _alignDuration);
            _dismountDuration = Mathf.Max(0f, _dismountDuration);
        }

        public void Configure(
            NavMeshLink link,
            EnemyTraversalKind kind,
            Vector3 localFacingDirection,
            float jumpArcHeight = 0.8f,
            float jumpSpeed = 4.5f,
            float alignDuration = 0.18f,
            float dismountDuration = 0.18f)
        {
            _link = link != null ? link : GetComponent<NavMeshLink>();
            _kind = kind;
            _localFacingDirection = localFacingDirection.sqrMagnitude > 0.0001f
                ? localFacingDirection.normalized
                : Vector3.forward;
            _jumpArcHeight = Mathf.Max(0f, jumpArcHeight);
            _jumpSpeed = Mathf.Max(0.1f, jumpSpeed);
            _alignDuration = Mathf.Max(0f, alignDuration);
            _dismountDuration = Mathf.Max(0f, dismountDuration);
        }
    }
}
