namespace PlatformerUltra.Enemies
{
    public enum EnemyTraversalKind
    {
        None,
        Ladder,
        Jump
    }

    public interface IEnemyTraversalMotor
    {
        bool IsTraversing { get; }
        EnemyTraversalKind ActiveTraversalKind { get; }
        float TraversalProgress { get; }
    }
}
