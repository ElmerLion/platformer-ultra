using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FactionMember))]
    public sealed class Targetable : MonoBehaviour, ITargetable
    {
        [SerializeField] private FactionMember _factionMember;
        [SerializeField] private TargetPoint _targetPoint;
        [SerializeField] private MonoBehaviour _damageableBehaviour;
        [SerializeField] private bool _targetable = true;

        private IDamageable _damageable;

        public GameObject TargetObject => gameObject;
        public Transform TargetPoint => _targetPoint != null ? _targetPoint.AimTransform : transform;
        public Faction Faction => _factionMember != null ? _factionMember.Faction : Faction.Neutral;
        public bool IsTargetable => _targetable && _damageable != null && _damageable.IsAlive;
        public IDamageable Damageable => _damageable;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        public void Configure(
            FactionMember factionMember,
            TargetPoint targetPoint,
            MonoBehaviour damageableBehaviour,
            bool targetable = true)
        {
            _factionMember = factionMember;
            _targetPoint = targetPoint;
            _damageableBehaviour = damageableBehaviour;
            _targetable = targetable;
            ResolveReferences();
        }

        public void SetTargetable(bool targetable)
        {
            _targetable = targetable;
        }

        private void ResolveReferences()
        {
            if (_factionMember == null)
            {
                _factionMember = GetComponent<FactionMember>();
            }

            _damageable = _damageableBehaviour as IDamageable;
        }
    }
}
