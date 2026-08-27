using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class DroneFlightMotor : MonoBehaviour, IEnemyMotor
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private Transform _visual;
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField, Min(0.1f)] private float _groundProbeDistance = 24f;
        [SerializeField, Min(0.05f)] private float _obstacleProbeRadius = 0.6f;
        [SerializeField, Min(0.001f)] private float _collisionSkin = 0.04f;
        [SerializeField, Range(15f, 90f)] private float _avoidanceAngle = 55f;
        [SerializeField, Min(0f)] private float _maximumBankAngle = 14f;

        private readonly RaycastHit[] _castHits = new RaycastHit[12];
        private Vector3 _destination;
        private Vector3 _velocity;
        private float _stoppingDistance = 4f;
        private bool _hasDestination;
        private bool _chasingPlayer;
        private bool _scriptedMotion;
        private float _desiredAltitude = float.NegativeInfinity;
        private Collider _bodyCollider;
        private Quaternion _visualBaseRotation = Quaternion.identity;
        private Vector3 _visualBaseLocalPosition;
        private float _bobPhase;

        public bool IsReady => enabled;
        public bool IsMoving => !_scriptedMotion && _velocity.sqrMagnitude > 0.01f;
        public Vector3 Velocity => _scriptedMotion ? Vector3.zero : _velocity;

        private void Awake()
        {
            _bodyCollider = GetComponent<Collider>();
            if (_visual != null)
            {
                _visualBaseRotation = _visual.localRotation;
                _visualBaseLocalPosition = _visual.localPosition;
            }

            _bobPhase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float timestamp)
        {
            if (_scriptedMotion)
            {
                return;
            }

            deltaTime = Mathf.Max(0f, deltaTime);
            if (!_hasDestination || deltaTime <= 0f)
            {
                _velocity = Vector3.zero;
                UpdateVisualBob(timestamp);
                UpdateBank(Vector3.zero, deltaTime);
                return;
            }

            Vector3 toDestination = _hasDestination ? _destination - transform.position : Vector3.zero;
            Vector3 planarOffset = Vector3.ProjectOnPlane(toDestination, Vector3.up);
            Vector3 desiredPlanarVelocity = Vector3.zero;
            if (_hasDestination && planarOffset.magnitude > _stoppingDistance)
            {
                Vector3 desiredDirection = ResolveAvoidanceDirection(
                    planarOffset.normalized,
                    Mathf.Min(1.5f, planarOffset.magnitude));
                desiredPlanarVelocity = desiredDirection * GetTravelSpeed();
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(_velocity, Vector3.up);
            float acceleration = desiredPlanarVelocity.sqrMagnitude > planarVelocity.sqrMagnitude
                ? GetAcceleration()
                : GetDeceleration();
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredPlanarVelocity, acceleration * deltaTime);

            float desiredHeight = ResolveHoverHeight();
            float verticalError = desiredHeight - transform.position.y;
            float desiredVerticalVelocity = Mathf.Clamp(verticalError * 4f, -GetTravelSpeed(), GetTravelSpeed());
            float verticalVelocity = Mathf.MoveTowards(_velocity.y, desiredVerticalVelocity, acceleration * deltaTime);
            _velocity = planarVelocity + Vector3.up * verticalVelocity;
            Vector3 previousPosition = transform.position;
            transform.position += ResolveCollisionConstrainedDisplacement(_velocity * deltaTime);
            _velocity = (transform.position - previousPosition) / deltaTime;

            Vector3 appliedPlanarVelocity = Vector3.ProjectOnPlane(_velocity, Vector3.up);
            if (appliedPlanarVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(appliedPlanarVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    GetRotationSpeed() * deltaTime);
            }

            UpdateVisualBob(timestamp);
            UpdateBank(appliedPlanarVelocity, deltaTime);
        }

        public void Configure(EnemyDefinition definition)
        {
            _definition = definition;
        }

        public void ConfigureVisual(Transform visual, LayerMask groundMask)
        {
            _visual = visual;
            _groundMask = groundMask;
            _visualBaseRotation = visual != null ? visual.localRotation : Quaternion.identity;
            _visualBaseLocalPosition = visual != null ? visual.localPosition : Vector3.zero;
            _bodyCollider = GetComponent<Collider>();
        }

        public bool TryPlace(Vector3 position, float searchRadius)
        {
            transform.position = position;
            _desiredAltitude = position.y;
            _velocity = Vector3.zero;
            _hasDestination = false;
            _scriptedMotion = false;
            return true;
        }

        public bool SetDestination(Vector3 position, float stoppingDistance, bool chasingPlayer)
        {
            _destination = position;
            _stoppingDistance = Mathf.Max(0.1f, stoppingDistance);
            _chasingPlayer = chasingPlayer;
            _desiredAltitude = position.y;
            _hasDestination = true;
            return true;
        }

        public bool CanReach(Vector3 position, float searchRadius)
        {
            return true;
        }

        public bool TryResolveLanding(Vector3 desiredPosition, float searchRadius, out Vector3 landingPosition)
        {
            landingPosition = desiredPosition;
            return true;
        }

        public void Stop()
        {
            _hasDestination = false;
            _velocity = Vector3.zero;
            if (_visual != null)
            {
                _visual.localRotation = _visualBaseRotation;
            }
        }

        public void FaceTarget(Vector3 targetPosition, float deltaTime)
        {
            Vector3 direction = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, GetRotationSpeed() * deltaTime);
        }

        public void BeginScriptedMotion()
        {
            _scriptedMotion = true;
            _velocity = Vector3.zero;
            _hasDestination = false;
        }

        public void SetScriptedPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void EndScriptedMotion(Vector3 landingPosition)
        {
            transform.position = landingPosition;
            _desiredAltitude = landingPosition.y;
            _velocity = Vector3.zero;
            _hasDestination = false;
            _scriptedMotion = false;
        }

        private Vector3 ResolveAvoidanceDirection(Vector3 desiredDirection, float probeDistance)
        {
            if (!TryCastBody(transform.position, desiredDirection, probeDistance, out RaycastHit obstacleHit))
            {
                return desiredDirection;
            }

            Vector3 slideDirection = Vector3.ProjectOnPlane(desiredDirection, obstacleHit.normal);
            slideDirection = Vector3.ProjectOnPlane(slideDirection, Vector3.up);
            if (slideDirection.sqrMagnitude > 0.01f)
            {
                slideDirection.Normalize();
                if (!TryCastBody(transform.position, slideDirection, probeDistance, out _))
                {
                    return slideDirection;
                }
            }

            Vector3 clockwise = Quaternion.AngleAxis(_avoidanceAngle, Vector3.up) * desiredDirection;
            if (!TryCastBody(transform.position, clockwise, probeDistance, out _))
            {
                return clockwise.normalized;
            }

            Vector3 counterClockwise = Quaternion.AngleAxis(-_avoidanceAngle, Vector3.up) * desiredDirection;
            if (!TryCastBody(transform.position, counterClockwise, probeDistance, out _))
            {
                return counterClockwise.normalized;
            }

            Vector3 right = Vector3.Cross(Vector3.up, desiredDirection).normalized;
            if (!TryCastBody(transform.position, right, probeDistance, out _))
            {
                return right;
            }

            Vector3 left = -right;
            if (!TryCastBody(transform.position, left, probeDistance, out _))
            {
                return left;
            }

            // Keep a deterministic non-zero steering intent even when boxed in.
            // The displacement cast below still prevents penetration.
            return clockwise.normalized;
        }

        private Vector3 ResolveCollisionConstrainedDisplacement(Vector3 displacement)
        {
            Vector3 startPosition = transform.position;
            Vector3 resolvedPosition = startPosition;
            Vector3 remaining = displacement;
            for (int iteration = 0; iteration < 2 && remaining.sqrMagnitude > 0.000001f; iteration++)
            {
                float distance = remaining.magnitude;
                Vector3 direction = remaining / distance;
                if (!TryCastBody(resolvedPosition, direction, distance + _collisionSkin, out RaycastHit hit))
                {
                    resolvedPosition += remaining;
                    break;
                }

                float travelDistance = Mathf.Clamp(hit.distance - _collisionSkin, 0f, distance);
                resolvedPosition += direction * travelDistance;
                float remainingDistance = Mathf.Max(0f, distance - travelDistance);
                Vector3 slideDirection = Vector3.ProjectOnPlane(direction, hit.normal);
                if (slideDirection.sqrMagnitude <= 0.0001f || remainingDistance <= 0.0001f)
                {
                    break;
                }

                remaining = slideDirection.normalized * remainingDistance;
            }

            return resolvedPosition - startPosition;
        }

        private bool TryCastBody(
            Vector3 rootPosition,
            Vector3 direction,
            float distance,
            out RaycastHit nearestHit)
        {
            nearestHit = default;
            if (direction.sqrMagnitude <= 0.000001f || distance <= 0f || _groundMask.value == 0)
            {
                return false;
            }

            direction.Normalize();
            int hitCount;
            if (_bodyCollider is BoxCollider boxCollider && boxCollider.enabled)
            {
                Vector3 scale = transform.lossyScale;
                Vector3 halfExtents = Vector3.Scale(
                    boxCollider.size * 0.5f,
                    new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                Vector3 worldCenterOffset = transform.rotation * Vector3.Scale(boxCollider.center, scale);
                hitCount = Physics.BoxCastNonAlloc(
                    rootPosition + worldCenterOffset,
                    halfExtents,
                    direction,
                    _castHits,
                    transform.rotation,
                    distance,
                    _groundMask,
                    QueryTriggerInteraction.Ignore);
            }
            else
            {
                hitCount = Physics.SphereCastNonAlloc(
                    rootPosition,
                    _obstacleProbeRadius,
                    direction,
                    _castHits,
                    distance,
                    _groundMask,
                    QueryTriggerInteraction.Ignore);
            }

            float nearestDistance = float.PositiveInfinity;
            bool found = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _castHits[index];
                if (hit.collider == null || IsOwnCollider(hit.collider) || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                nearestHit = hit;
                found = true;
            }

            return found;
        }

        private bool IsOwnCollider(Collider candidate)
        {
            Transform candidateTransform = candidate.transform;
            return candidateTransform == transform || candidateTransform.IsChildOf(transform);
        }

        private float ResolveHoverHeight()
        {
            float hoverHeight = transform.position.y;
            Vector3 origin = transform.position + Vector3.up * 2f;
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    _groundProbeDistance,
                    _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                hoverHeight = hit.point.y + (_definition != null ? _definition.HoverHeight : 2.5f);
            }

            // A machine can be several factory tiers above the drone. Retain the
            // destination altitude even after Stop() is called for an attack so the
            // drone does not descend out of range and oscillate between floors.
            return float.IsNegativeInfinity(_desiredAltitude)
                ? hoverHeight
                : Mathf.Max(hoverHeight, _desiredAltitude);
        }

        private void UpdateBank(Vector3 planarVelocity, float deltaTime)
        {
            if (_visual == null)
            {
                return;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);
            float speed = Mathf.Max(0.1f, GetTravelSpeed());
            float bank = Mathf.Clamp(-localVelocity.x / speed, -1f, 1f) * _maximumBankAngle;
            Quaternion target = _visualBaseRotation * Quaternion.Euler(0f, 0f, bank);
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, target, 1f - Mathf.Exp(-7f * deltaTime));
        }

        private void UpdateVisualBob(float timestamp)
        {
            if (_visual == null)
            {
                return;
            }

            float bob = Mathf.Sin((timestamp + _bobPhase) * GetBobFrequency() * Mathf.PI * 2f) * GetBobAmplitude();
            _visual.localPosition = _visualBaseLocalPosition + Vector3.up * bob;
        }

        private float GetTravelSpeed()
        {
            if (_definition == null)
            {
                return 3.8f;
            }

            return _chasingPlayer ? _definition.PlayerChaseSpeed : _definition.MachineTravelSpeed;
        }

        private float GetAcceleration()
        {
            return _definition != null ? _definition.Acceleration : 7f;
        }

        private float GetDeceleration()
        {
            return _definition != null ? _definition.Deceleration : 9f;
        }

        private float GetRotationSpeed()
        {
            return _definition != null ? _definition.RotationSpeed : 240f;
        }

        private float GetBobAmplitude()
        {
            return _definition != null ? _definition.BobAmplitude : 0.12f;
        }

        private float GetBobFrequency()
        {
            return _definition != null ? _definition.BobFrequency : 1.4f;
        }
    }
}
