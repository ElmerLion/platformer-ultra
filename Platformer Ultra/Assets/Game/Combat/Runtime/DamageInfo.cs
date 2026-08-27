using System;
using UnityEngine;

namespace PlatformerUltra.Combat
{
    [Serializable]
    public struct DamageInfo
    {
        [SerializeField, Min(0)] private int _amount;
        [SerializeField] private GameObject _source;
        [SerializeField] private Faction _sourceFaction;
        [SerializeField] private Vector3 _hitPoint;

        public DamageInfo(int amount, GameObject source, Faction sourceFaction, Vector3 hitPoint)
        {
            _amount = Mathf.Max(0, amount);
            _source = source;
            _sourceFaction = sourceFaction;
            _hitPoint = hitPoint;
        }

        public int Amount => _amount;
        public GameObject Source => _source;
        public Faction SourceFaction => _sourceFaction;
        public Vector3 HitPoint => _hitPoint;
    }
}
