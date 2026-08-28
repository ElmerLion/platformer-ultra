using UnityEngine;
using UnityEngine.AI;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshEnemyMotor : MonoBehaviour, IEnemyMotor, IEnemyTraversalMotor
    {
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private EnemyDefinition _definition;

        private NavMeshPath _probePath;
        private bool _scriptedMotion;
        private EnemyTraversalLink _activeTraversalLink;
        private Vector3 _traversalAlignmentOrigin;
        private Vector3 _traversalStart;
        private Vector3 _traversalEnd;
        private Vector3 _traversalFacingDirection;
        private float _traversalElapsed;
        private float _traversalTravelDuration;
        private bool _agentUpdatePositionBeforeTraversal;
        private bool _agentUpdateRotationBeforeTraversal;
        private bool _agentStoppedBeforeTraversal;

        public bool IsReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;
        public bool IsMoving => IsTraversing ||
                                (IsReady && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.01f);
        public Vector3 Velocity => IsReady && !_scriptedMotion && !IsTraversing
            ? _agent.velocity
            : Vector3.zero;
        public bool IsTraversing => _activeTraversalLink != null;
        public EnemyTraversalKind ActiveTraversalKind => IsTraversing
            ? _activeTraversalLink.Kind
            : EnemyTraversalKind.None;
        public float TraversalProgress { get; private set; }

        private void Awake()
        {
            _probePath = new NavMeshPath();
            if (_agent == null)
            {
                _agent = GetComponent<NavMeshAgent>();
            }

            ApplyDefinition();
        }

        private void Update()
        {
            if (_scriptedMotion)
            {
                return;
            }

            if (IsTraversing)
            {
                TickTraversal(Time.deltaTime);
                return;
            }

            if (!IsReady)
            {
                return;
            }

            if (_agent.isOnOffMeshLink && TryBeginTraversal())
            {
                TickTraversal(Time.deltaTime);
                return;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(_agent.velocity, Vector3.up);
            if (planarVelocity.sqrMagnitude <= 0.01f)
            {
                return;
            }

            RotateTowards(planarVelocity.normalized, Time.deltaTime);
        }

        public void Configure(EnemyDefinition definition)
        {
            _definition = definition;
            ApplyDefinition();
        }

        public bool TryPlace(Vector3 position, float searchRadius)
        {
            if (_agent == null || !_agent.enabled ||
                !NavMesh.SamplePosition(position, out NavMeshHit hit, Mathf.Max(0.25f, searchRadius), NavMesh.AllAreas))
            {
                return false;
            }

            return _agent.Warp(hit.position);
        }

        public bool SetDestination(Vector3 position, float stoppingDistance, bool chasingPlayer)
        {
            if (IsTraversing)
            {
                return true;
            }

            if (!IsReady)
            {
                return false;
            }

            float searchRadius = Mathf.Max(1f, stoppingDistance * 1.4f);
            if (!TryFindReachablePoint(position, searchRadius, out Vector3 resolvedPosition))
            {
                return false;
            }

            _agent.speed = GetTravelSpeed(chasingPlayer);
            _agent.stoppingDistance = Mathf.Max(0.05f, stoppingDistance);
            _agent.isStopped = false;
            return _agent.SetDestination(resolvedPosition);
        }

        public bool CanReach(Vector3 position, float searchRadius)
        {
            return IsReady && TryFindReachablePoint(position, Mathf.Max(0.5f, searchRadius), out _);
        }

        public bool TryResolveLanding(Vector3 desiredPosition, float searchRadius, out Vector3 landingPosition)
        {
            landingPosition = desiredPosition;
            if (!NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit hit,
                    Mathf.Max(0.5f, searchRadius),
                    NavMesh.AllAreas))
            {
                return false;
            }

            landingPosition = hit.position;
            return !_agent.enabled || !_agent.isOnNavMesh || TryFindReachablePoint(hit.position, searchRadius, out landingPosition);
        }

        public void Stop()
        {
            if (IsTraversing)
            {
                CancelTraversal(true);
            }

            if (!IsReady || _scriptedMotion)
            {
                return;
            }

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public void FaceTarget(Vector3 targetPosition, float deltaTime)
        {
            if (IsTraversing)
            {
                return;
            }

            Vector3 direction = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.0001f)
            {
                RotateTowards(direction.normalized, deltaTime);
            }
        }

        public void BeginScriptedMotion()
        {
            if (_agent == null)
            {
                return;
            }

            if (IsTraversing)
            {
                CancelTraversal(true);
            }

            if (IsReady)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.updatePosition = false;
            }

            _scriptedMotion = true;
        }

        public void SetScriptedPosition(Vector3 position)
        {
            transform.position = position;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.nextPosition = position;
            }
        }

        public void EndScriptedMotion(Vector3 landingPosition)
        {
            if (_agent != null && _agent.enabled)
            {
                _agent.updatePosition = true;
                if (_agent.isOnNavMesh)
                {
                    _agent.Warp(landingPosition);
                    _agent.isStopped = true;
                }
            }

            transform.position = landingPosition;
            _scriptedMotion = false;
        }

        private void OnDisable()
        {
            if (IsTraversing)
            {
                CancelTraversal(true);
            }
        }

        private bool TryBeginTraversal()
        {
            OffMeshLinkData data = _agent.currentOffMeshLinkData;
            if (!data.valid)
            {
                return false;
            }

            Component owner = data.owner as Component;
            EnemyTraversalLink traversalLink = owner != null
                ? owner.GetComponent<EnemyTraversalLink>()
                : null;
            if (traversalLink == null || traversalLink.Kind == EnemyTraversalKind.None)
            {
                _agent.CompleteOffMeshLink();
                return false;
            }

            Vector3 first = data.startPos;
            Vector3 second = data.endPos;
            if ((transform.position - second).sqrMagnitude < (transform.position - first).sqrMagnitude)
            {
                (first, second) = (second, first);
            }

            _activeTraversalLink = traversalLink;
            _traversalAlignmentOrigin = transform.position;
            _traversalStart = first;
            _traversalEnd = second;
            bool ascending = second.y >= first.y;
            _traversalFacingDirection = traversalLink.Kind == EnemyTraversalKind.Ladder
                ? (ascending ? traversalLink.FacingDirectionWorld : -traversalLink.FacingDirectionWorld)
                : Vector3.ProjectOnPlane(second - first, Vector3.up).normalized;
            if (_traversalFacingDirection.sqrMagnitude <= 0.0001f)
            {
                _traversalFacingDirection = transform.forward;
            }

            float travelDistance = Vector3.Distance(first, second);
            if (traversalLink.Kind == EnemyTraversalKind.Ladder)
            {
                float baseSpeed = _definition != null ? _definition.MachineTravelSpeed : 2f;
                float climbSpeed = Mathf.Clamp(baseSpeed * 0.8f, 1.2f, 2f);
                _traversalTravelDuration = travelDistance / climbSpeed;
            }
            else
            {
                _traversalTravelDuration = Mathf.Clamp(
                    travelDistance / traversalLink.JumpSpeed,
                    0.45f,
                    0.9f);
            }

            _traversalTravelDuration = Mathf.Max(0.05f, _traversalTravelDuration);
            _traversalElapsed = 0f;
            TraversalProgress = 0f;
            _agentUpdatePositionBeforeTraversal = _agent.updatePosition;
            _agentUpdateRotationBeforeTraversal = _agent.updateRotation;
            _agentStoppedBeforeTraversal = _agent.isStopped;
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.isStopped = false;
            return true;
        }

        private void TickTraversal(float deltaTime)
        {
            if (!IsTraversing || _agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                CancelTraversal(true);
                return;
            }

            if (!_agent.isOnOffMeshLink)
            {
                RestoreAgentAfterTraversal();
                return;
            }

            float step = Mathf.Max(0f, deltaTime);
            _traversalElapsed += step;
            float alignDuration = _activeTraversalLink.AlignDuration;
            float dismountDuration = _activeTraversalLink.DismountDuration;
            float travelStart = alignDuration;
            float travelEnd = travelStart + _traversalTravelDuration;
            float totalDuration = travelEnd + dismountDuration;

            Vector3 position;
            if (_traversalElapsed < travelStart && alignDuration > 0f)
            {
                float alignment = Mathf.Clamp01(_traversalElapsed / alignDuration);
                position = Vector3.LerpUnclamped(
                    _traversalAlignmentOrigin,
                    _traversalStart,
                    alignment * alignment * (3f - 2f * alignment));
                TraversalProgress = 0f;
            }
            else if (_traversalElapsed < travelEnd)
            {
                float progress = Mathf.Clamp01(
                    (_traversalElapsed - travelStart) / _traversalTravelDuration);
                TraversalProgress = progress;
                position = _activeTraversalLink.Kind == EnemyTraversalKind.Ladder
                    ? EnemyTraversalMotion.EvaluateLadder(_traversalStart, _traversalEnd, progress)
                    : EnemyTraversalMotion.EvaluateJump(
                        _traversalStart,
                        _traversalEnd,
                        progress,
                        _activeTraversalLink.JumpArcHeight);
            }
            else
            {
                position = _traversalEnd;
                TraversalProgress = 1f;
            }

            transform.position = position;
            _agent.nextPosition = position;
            RotateTowards(_traversalFacingDirection, step);

            if (_traversalElapsed >= totalDuration)
            {
                CompleteTraversal(_traversalEnd, false);
            }
        }

        private void CancelTraversal(bool useNearestEndpoint)
        {
            if (!IsTraversing)
            {
                return;
            }

            Vector3 landing = useNearestEndpoint
                ? EnemyTraversalMotion.GetNearestEndpoint(
                    transform.position,
                    _traversalStart,
                    _traversalEnd)
                : _traversalEnd;

            CompleteTraversal(landing, true);
        }

        private void CompleteTraversal(Vector3 landing, bool resetPath)
        {
            if (_agent != null && _agent.enabled)
            {
                if (_agent.isOnOffMeshLink)
                {
                    _agent.nextPosition = _traversalEnd;
                    _agent.CompleteOffMeshLink();
                }

                _agent.updatePosition = _agentUpdatePositionBeforeTraversal;
                _agent.updateRotation = _agentUpdateRotationBeforeTraversal;
                if (_agent.isOnNavMesh)
                {
                    _agent.Warp(landing);
                    _agent.isStopped = resetPath || _agentStoppedBeforeTraversal;
                    if (resetPath)
                    {
                        _agent.ResetPath();
                    }
                }
            }

            transform.position = landing;
            RestoreTraversalState();
        }

        private void RestoreAgentAfterTraversal()
        {
            if (_agent != null && _agent.enabled)
            {
                _agent.updatePosition = _agentUpdatePositionBeforeTraversal;
                _agent.updateRotation = _agentUpdateRotationBeforeTraversal;
                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = _agentStoppedBeforeTraversal;
                }
            }

            RestoreTraversalState();
        }

        private void RestoreTraversalState()
        {
            _activeTraversalLink = null;
            _traversalElapsed = 0f;
            _traversalTravelDuration = 0f;
            TraversalProgress = 0f;
        }

        private bool TryFindReachablePoint(Vector3 position, float searchRadius, out Vector3 resolvedPosition)
        {
            resolvedPosition = position;
            _probePath ??= new NavMeshPath();

            float radius = Mathf.Max(0.25f, searchRadius);
            if (TryFindReachableSample(position, radius, out resolvedPosition))
            {
                return true;
            }

            // A machine's closest NavMesh polygon can be an isolated roof even though
            // the surrounding deck is reachable. Probe downward only after the nearest
            // sample fails so elevated targets still resolve to their own tier.
            Vector3 lowerProbe = position + Vector3.down * Mathf.Min(radius, 2.5f);
            return TryFindReachableSample(lowerProbe, radius, out resolvedPosition);
        }

        private bool TryFindReachableSample(Vector3 position, float searchRadius, out Vector3 resolvedPosition)
        {
            resolvedPosition = position;
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            {
                return false;
            }

            if (!_agent.CalculatePath(hit.position, _probePath) || _probePath.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            resolvedPosition = hit.position;
            return true;
        }

        private float GetTravelSpeed(bool chasingPlayer)
        {
            if (_definition == null)
            {
                return _agent != null ? _agent.speed : 0f;
            }

            return chasingPlayer ? _definition.PlayerChaseSpeed : _definition.MachineTravelSpeed;
        }

        private void ApplyDefinition()
        {
            if (_agent == null || _definition == null)
            {
                return;
            }

            _agent.speed = _definition.MachineTravelSpeed;
            _agent.acceleration = _definition.Acceleration;
            _agent.angularSpeed = _definition.RotationSpeed;
            _agent.updateRotation = false;
            _agent.updateUpAxis = true;
            _agent.autoBraking = true;
            _agent.autoRepath = true;
            _agent.autoTraverseOffMeshLink = false;
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            float degrees = (_definition != null ? _definition.RotationSpeed : 540f) * deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, degrees);
        }
    }
}
