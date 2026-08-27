using UnityEngine;

namespace PlatformerUltra.Combat
{
    public interface ITargetable
    {
        GameObject TargetObject { get; }
        Transform TargetPoint { get; }
        Faction Faction { get; }
        bool IsTargetable { get; }
        IDamageable Damageable { get; }
    }
}
