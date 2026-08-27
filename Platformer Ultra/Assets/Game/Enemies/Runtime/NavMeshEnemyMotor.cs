using UnityEngine;
using UnityEngine.AI;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshEnemyMotor : MonoBehaviour, IEnemyMotor
    {
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private EnemyDefinition _definition;

        private NavMeshPath _probePath;
        private bool _scriptedMotion;

        public bool IsReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;
        public bool IsMoving => IsReady && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.01f;
        public Vector3 Velocity => IsReady && !_scriptedMotion ? _agent.velocity : Vector3.zero;

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
            if (_scriptedMotion || !IsReady)
            {
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
            if (!IsReady || _scriptedMotion)
            {
                return;
            }

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public void FaceTarget(Vector3 targetPosition, float deltaTime)
        {
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
            _agent.autoTraverseOffMeshLink = true;
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            float degrees = (_definition != null ? _definition.RotationSpeed : 540f) * deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, degrees);
        }
    }
}
