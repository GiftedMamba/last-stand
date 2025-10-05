namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Distinguishes enemy kinds for configuration, logic, and UI. Keep Unknown = 0.
    /// </summary>
    public enum EnemyType
    {
        Unknown = 0,
        Ghoul = 1,
        Skeleton = 2,
        Goblin = 3,
        BigGhoul = 4,
        BigSkeleton = 5,
        BigGoblin = 6,
        ShamanGoblin = 7,
        BossSkeleton = 8,
        BossGoblin = 9,
        BossGhoul = 10,
    }
}