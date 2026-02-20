using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Tower : Node2D
{
    private Vector2I TargetPosition = Vector2I.MinValue;
    private Vector2 TargetVelocity;
    private Vector2I[] TargetCoordinates;


    private FieldHandler fieldHandler;
    private EnemyDirector director;
    private SpatialEntityGrid entityGrid;

    public Area2D AttackArea { get; private set; }
    private PhysicsShapeQueryParameters2D _query;

    private Timer TargetTimer, FireTimer;

    private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes/Player/Projectile.tscn");

    public Tower(Vector2 Position, float TargetRadius, float AttackRadius, FieldHandler fieldHandler, EnemyDirector director, SpatialEntityGrid entityGrid)
    {
        this.Position = Position;
        this.fieldHandler = fieldHandler;
        this.entityGrid = entityGrid;
        this.director = director;
        ZIndex = 50;
        TargetCoordinates = FindTargetCoordinates(TargetRadius);
        BuildArea(AttackRadius);

        //base.AddChild(AttackArea);

        TargetTimer = new Timer();
        TargetTimer.WaitTime = 0.5f;
        TargetTimer.Autostart = true;
        TargetTimer.Timeout += FindTarget;
        base.AddChild(TargetTimer);


        FireTimer = new Timer();
        FireTimer.WaitTime = 0.01f;
        FireTimer.Autostart = true;
        FireTimer.Timeout += FireWeapon;
        base.AddChild(FireTimer);

        GD.Print(Position);
    }

    public void FireWeapon()
    {
        if (TargetPosition == Vector2I.MinValue)
            return;

        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.ZIndex = 10;
        projectile.speed = 20;
        projectile.OnEnemyHit += OnEnemyHit;
        var targetPos = fieldHandler.MapToLocal(TargetPosition);
        projectile.Velocity = Position.DirectionTo(CalculateIntercept(projectile.speed, targetPos, TargetVelocity));
        base.AddChild(projectile);
    }

    public void OnEnemyHit(Array<Rid> enemies)
    {
        foreach (var enemy in enemies)
            director.DamageEnemy(enemy, 50);
    }

    public void FindTarget()
    {
        foreach (var coord in TargetCoordinates)
            if (fieldHandler.GetDensityAt(coord) > 0)
            {
                TargetPosition = coord;
                var index = entityGrid.GetFirstInCell(coord);
                if (index >= 0)
                    TargetVelocity = director.GetEnemyAt(index).Velocity;
                else
                    TargetVelocity = Vector2.Zero;
                return;
            }
        TargetPosition = Vector2I.MinValue;
    }

    private void BuildArea(float radius)
    {
        var shape = new CircleShape2D();
        shape.Radius = radius;

        var collision = new CollisionShape2D();
        collision.Shape = shape;

        Area2D AttackArea = new Area2D();
        AttackArea.AddChild(collision);

        _query = new PhysicsShapeQueryParameters2D
        {
            ShapeRid = shape.GetRid(),
            CollisionMask = 2,
            CollideWithAreas = false,
            CollideWithBodies = true
        };
    }

    private Vector2 CalculateIntercept(float projectileSpeed, Vector2 enemyPos, Vector2 enemyVelocity)
    {
        // The vector from the tower to the enemy
        Vector2 relativePos = enemyPos - Position;

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

    private Vector2I[] FindTargetCoordinates(float radius)
    {
        var cells = GetRelativeIntersectingCells(this.fieldHandler.CellDimensions.X, radius).Select(vec => fieldHandler.GetCellAt(vec + fieldHandler.LocalToMap(this.Position))).ToArray();
        cells = cells.OrderBy(cell => cell.CalculatedWeight).ToArray();
        var coords = new Array<Vector2I>();
        foreach (var cell in cells)
            coords.Add(cell.Position);
        return coords.ToArray();
    }
    private static Array<Vector2I> GetRelativeIntersectingCells(float gridSize, float radius)
    {
        Array<Vector2I> result = new Array<Vector2I>();

        float gridRadius = radius / gridSize;
        float gridRadiusSquared = gridRadius * gridRadius;

        for (int i = 0; i <= (int)gridRadius; i++)
        {
            int distance = (int)Math.Sqrt(gridRadiusSquared - (i * i));
            for (int j = 0 - distance; j <= distance; j++)
            {
                result.Add(new Vector2I(j, i));
                if (i > 0)
                {
                    result.Add(new Vector2I(j, -i));
                }
            }
        }
        return result;
    }
}
