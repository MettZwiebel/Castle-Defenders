/*

using CastleDefender.Scripts.World;
using Godot;

public partial class Tower : Node2D
{

    [Export]
    public TargetingArea Area;
    [Export]
    public float TargetAreaSize = 512;
    private PackedScene projectileScene = GD.Load<PackedScene>("res://Scenes/Player/Projectile.tscn");
    private Vector2I? Target;
    private FieldHandler FieldHandler;

    public override void _Ready()
    {
        Area.parent = this;
        Area.Prepare(32f, TargetAreaSize);
    }

    public void OnTargetTimerTimeout()
    {
        Target = Area.GetAttackTarget();
    }
    public void OnShootingTimerTimeout()
    {
        ShootAtTarget();
    }

    public void ShootAtTarget()
    {
        if (Target == null)
        {
            return;
        }
        var vector = (Vector2I)Target;
        var enemy = FieldHandler.GetEnemyAt(vector);
        var projectile = (Projectile)projectileScene.Instantiate();
        projectile.speed = 50;
        var dir = Vector2.Zero;
        if (IsInstanceValid(enemy))
        {
            var pos = CalculateIntercept(Position, projectile.speed, enemy.Position, enemy.Velocity);
            dir = Position.DirectionTo(pos);
        }
        else
        {
            dir = Position.DirectionTo(FieldHandler.MapToLocal((Vector2I)Target));
        }
        projectile.Velocity = dir;
        base.AddChild(projectile);
    }

    private Vector2 CalculateIntercept(Vector2 towerPos, float projectileSpeed, Vector2 enemyPos, Vector2 enemyVelocity)
    {
        // The vector from the tower to the enemy
        Vector2 relativePos = enemyPos - towerPos;

        // Coefficients for the quadratic equation: at^2 + bt + c = 0
        // a = (Vx^2 + Vy^2) - projectileSpeed^2
        float a = enemyVelocity.Dot(enemyVelocity) - (projectileSpeed * projectileSpeed);
        // b = 2 * (relativePos.x * enemyVelocity.x + relativePos.y * enemyVelocity.y)
        float b = 2f * enemyVelocity.Dot(relativePos);
        // c = relativePos.x^2 + relativePos.y^2
        float c = relativePos.Dot(relativePos);

        float determinant = b * b - 4f * a * c;

        if (determinant < 0)
        {
            // No solution: The enemy is moving too fast away from the tower
            return enemyPos;
        }

        float sqrtDet = Mathf.Sqrt(determinant);
        float t1 = (-b + sqrtDet) / (2f * a);
        float t2 = (-b - sqrtDet) / (2f * a);

        // We need the smallest positive time 't'
        float t;
        if (t1 > 0 && t2 > 0) t = Mathf.Min(t1, t2);
        else if (t1 > 0) t = t1;
        else if (t2 > 0) t = t2;
        else return enemyPos; // Intercept is in the past

        // Intercept Point = Current Position + (Direction * Speed * Time)
        return enemyPos + (enemyVelocity * t);
    }

}

*/