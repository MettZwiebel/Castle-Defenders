using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class DeathZone : Area2D
{
    public EnemyDirector director;
    [Export] public float DamageRadius = 200f;  // Zone radius in world units
    [Export] public int DamageAmount = 500;     // Damage per hit
    
    public override void _Ready()
    {
        // No longer need physics shape querying since we use spatial grid
    }

    public void OnTimeout()
    {
        // Damage all enemies within DamageRadius of this zone's center
        director.DamageEnemiesInArea(GlobalPosition, DamageRadius, DamageAmount);
    }
}

