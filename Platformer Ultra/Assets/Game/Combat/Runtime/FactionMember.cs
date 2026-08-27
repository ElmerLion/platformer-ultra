using UnityEngine;

namespace PlatformerUltra.Combat
{
    [DisallowMultipleComponent]
    public sealed class FactionMember : MonoBehaviour
    {
        [SerializeField] private Faction _faction = Faction.Neutral;

        public Faction Faction => _faction;

        public void Configure(Faction faction)
        {
            _faction = faction;
        }
    }
}
