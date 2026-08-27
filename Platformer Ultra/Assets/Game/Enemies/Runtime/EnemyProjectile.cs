using PlatformerUltra.Combat;
using UnityEngine;

namespace PlatformerUltra.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.02f)] private float _impactDistance = 0.22f;
        [SerializeField, Min(0f)] private float _spinSpeed = 420f;
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField] private GameObject _impactEffectPrefab;
        [SerializeField, Min(0.1f)] private float _impactEffectScale = 1f;

        private readonly RaycastHit[] _castHits = new RaycastHit[8];
        private GameObject _source;
        private Targetable _target;
        private Vector3 _direction;
        private float _remainingDistance;
        private int _damage;
        private float _speed;
        private float _expiresAt;
        private bool _resolved;

        public bool IsResolved => _resolved;

        public void ConfigureImpactEffect(GameObject impactEffectPrefab, float scale = 1f)
        {
            _impactEffectPrefab = impactEffectPrefab;
            _impactEffectScale = Mathf.Max(0.1f, scale);
        }

        public void Initialize(
            GameObject source,
            Targetable target,
            int damage,
            float speed,
            float lifetime)
        {
            _source = source;
            _target = target;
            _damage = Mathf.Max(0, damage);
            _speed = Mathf.Max(0.1f, speed);
            _expiresAt = Time.time + Mathf.Max(0.1f, lifetime);
            Vector3 offset = target != null
                ? target.TargetPoint.position - transform.position
                : transform.forward;
            _remainingDistance = Mathf.Max(_impactDistance, offset.magnitude);
            _direction = offset.sqrMagnitude > 0.000001f ? offset.normalized : transform.forward;
            if (_direction.sqrMagnitude <= 0.000001f)
            {
                _direction = Vector3.forward;
            }

            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float timestamp)
        {
            if (_resolved)
            {
                return;
            }

            if (_target == null || !_target.IsTargetable || timestamp >= _expiresAt)
            {
                ResolveWithoutDamage();
                return;
            }

            float step = Mathf.Min(_speed * Mathf.Max(0f, deltaTime), _remainingDistance);
            if (step <= 0f)
            {
                return;
            }

            if (TryFindCollision(step, out RaycastHit hit))
            {
                transform.position += _direction * Mathf.Max(0f, hit.distance);
                if (IsCapturedTargetCollider(hit.collider))
                {
                    ApplyDamage();
                }
                else
                {
                    ResolveWithoutDamage();
                }

                return;
            }

            transform.position += _direction * step;
            _remainingDistance -= step;
            transform.Rotate(Vector3.forward, _spinSpeed * deltaTime, Space.Self);
            if (_remainingDistance <= 0.0001f)
            {
                Vector3 targetOffset = _target.TargetPoint.position - transform.position;
                if (targetOffset.sqrMagnitude <= _impactDistance * _impactDistance)
                {
                    ApplyDamage();
                }
                else
                {
                    ResolveWithoutDamage();
                }
            }
        }

        private bool TryFindCollision(float distance, out RaycastHit nearestHit)
        {
            nearestHit = default;
            int hitCount = Physics.SphereCastNonAlloc(
                transform.position,
                _impactDistance,
                _direction,
                _castHits,
                distance,
                _collisionMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            bool found = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = _castHits[index];
                if (hit.collider == null || IsSourceCollider(hit.collider) || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                nearestHit = hit;
                found = true;
            }

            return found;
        }

        private bool IsSourceCollider(Collider candidate)
        {
            if (_source == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            Transform sourceTransform = _source.transform;
            return candidateTransform == sourceTransform || candidateTransform.IsChildOf(sourceTransform);
        }

        private bool IsCapturedTargetCollider(Collider candidate)
        {
            if (_target == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            Transform targetTransform = _target.transform;
            return candidateTransform == targetTransform || candidateTransform.IsChildOf(targetTransform);
        }

        private void ApplyDamage()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            IDamageable damageable = _target != null ? _target.Damageable : null;
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(new DamageInfo(_damage, _source, Faction.Enemy, transform.position));
            }

            SpawnImpactEffect();
            RemoveProjectile();
        }

        private void ResolveWithoutDamage()
        {
            _resolved = true;
            SpawnImpactEffect();
            RemoveProjectile();
        }

        private void SpawnImpactEffect()
        {
            if (_impactEffectPrefab == null || !Application.isPlaying)
            {
                return;
            }

            GameObject instance = Instantiate(_impactEffectPrefab, transform.position, Quaternion.identity);
            instance.transform.localScale = Vector3.one * _impactEffectScale;
            instance.GetComponent<GameplayEffect>()?.Play();
        }

        private void RemoveProjectile()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
