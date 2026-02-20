using System;

public class EnemyStats
{
    public int Health { get; set; } = 100;
    public int DamageToTowers { get; set; } = 10;
    public int DamageToGates { get; set; } = 10;
    public int Speed { get; set; } = 100;
    public EnemyType EnemyType { get; set; } = EnemyType.MELEE;


    public static EnemyStats GetRandomizedStats(EnemyType type, Random rand)
    {
        var stats = new EnemyStats();
        switch (type)
        {
            case EnemyType.MELEE:
                stats.Health = rand.Next(80, 150);
                stats.DamageToTowers = 0;
                stats.Speed = rand.Next(75, 125);
                break;
            case EnemyType.RANGED:
                break;
            case EnemyType.ELITE:
                break;
        }
        return stats;
    }
}