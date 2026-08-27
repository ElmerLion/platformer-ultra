namespace PlatformerUltra.Enemies
{
    public static class ArmoredSpecialAttackPolicy
    {
        public static bool IsEligible(
            float currentTime,
            float lastSpecialTime,
            float cooldown,
            float distance,
            float minimumDistance,
            float maximumDistance,
            bool pathAndLandingReasonable)
        {
            return pathAndLandingReasonable &&
                   currentTime >= lastSpecialTime + cooldown &&
                   distance >= minimumDistance &&
                   distance <= maximumDistance;
        }
    }
}
