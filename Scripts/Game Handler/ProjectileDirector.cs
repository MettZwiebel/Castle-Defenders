using CastleDefender.Scripts.World;
using Godot;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class ProjectileDirector : Node2D
{
    private ProjectileData[] _pool;
    private int[] damageBuffer;
    private int _activeCount = 0;
    private const int MaxProjectiles = 10000;

    private FieldHandler fieldHandler;
    private SpatialEntityGrid entityGrid;
    private EnemyDirector director;

    [Export] private MultiMeshInstance2D _multiMesh;


    public ProjectileDirector(FieldHandler fieldHandler, SpatialEntityGrid entityGrid, EnemyDirector director) : base()
    {
        this.fieldHandler = fieldHandler;
        this.entityGrid = entityGrid;
        this.director = director;

        damageBuffer = new int[director.EnemyMaximum];
    }
    private ParallelOptions _parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = Mathf.Max(1, System.Environment.ProcessorCount / 2)
    };

    public override void _Ready()
    {
        _pool = new ProjectileData[MaxProjectiles];
        // Pre-configure the MultiMesh to handle the max count
        _multiMesh.Multimesh.InstanceCount = MaxProjectiles;
        _multiMesh.Multimesh.VisibleInstanceCount = 0;
    }

    public void Spawn(Vector2 pos, Vector2 dir, float speed, float dist)
    {
        if (_activeCount >= MaxProjectiles) return;

        // Initialize at the end of the active section
        _pool[_activeCount] = new ProjectileData
        {
            Position = pos,
            Direction = dir,
            Speed = speed,
            MaxDistance = dist,
            IsAlive = true
            // ... add sprite index / effects here
        };

        _activeCount++;
    }

    public override void _PhysicsProcess(double delta)
    {
        Parallel.For(0, _activeCount, _parallelOptions, i =>
        {
            UpdateProjectile(i, (float)delta);
        });

        // 2. Sequential Cleanup & Visual Sync
        CleanupAndSync();
    }

    private void CleanupAndSync()
    {
        for (int i = 0; i < damageBuffer.Length; i++)
            if (damageBuffer[i] > 0)
            {
                director.DamageEnemy(i, damageBuffer[i]);
                damageBuffer[i] = 0;
            }

        for (int i = 0; i < _activeCount; i++)
        {
            if (!_pool[i].IsAlive)
            {
                // Swap with the last active projectile
                _pool[i] = _pool[_activeCount - 1];
                _activeCount--;

                // Re-check this index since a new projectile was moved into it
                i--;
                continue;
            }

            // 3. Update Visuals (MultiMesh)
            // We do this while iterating for cleanup to save a second loop
            Transform2D xform = new Transform2D(0, _pool[i].Position);
            _multiMesh.Multimesh.SetInstanceTransform2D(i, xform);

            // Pass extra data (Sprite index, effects) to the Uber Shader
            // _multiMesh.Multimesh.SetInstanceCustomData(i, _pool[i].ShaderData);
        }

        _multiMesh.Multimesh.VisibleInstanceCount = _activeCount;
    }

    private void UpdateProjectile(int i, float delta)
    {
        ref var projectile = ref _pool[i];
        projectile.Position += projectile.Direction * projectile.Speed * delta;
        projectile.DistanceTraveled += projectile.Speed * delta;

        if (projectile.DistanceTraveled >= projectile.MaxDistance)
        {
            projectile.IsAlive = false;
            return;
        }
        if (projectile.HitCooldownFrames > 0)
        {
            projectile.HitCooldownFrames--;
            return;
        }

        // Grid Collision Check
        // Convert Position to Cell Index
        Vector2I cell = fieldHandler.LocalToMap(projectile.Position);
        int enemyIdx = entityGrid.GetFirstInCell(cell);

        while (enemyIdx != -1)
        {
            if (CheckHit(projectile, enemyIdx))
            {
                HandleHit(ref projectile, enemyIdx);
                if (projectile.PenetrationCount <= 0)
                {
                    projectile.IsAlive = false;
                    break;
                }
            }
            enemyIdx = entityGrid.GetNext(enemyIdx);
        }
    }

    private bool CheckHit(in ProjectileData p, int enemyIdx)
    {
        var enemy = director.GetEnemyAt(enemyIdx);

        // Simple Circle-to-Circle collision using Squared Distance
        float dx = p.Position.X - enemy.Position.X;
        float dy = p.Position.Y - enemy.Position.Y;
        float distanceSquared = (dx * dx) + (dy * dy);

        // (RadiusA + RadiusB)^2
        float collisionThreshold = p.Radius + enemy.Radius;
        return distanceSquared <= (collisionThreshold * collisionThreshold);
    }

    private void HandleHit(ref ProjectileData p, int enemyIdx)
    {
        // 1. Apply Damage (Thread-safe if using Parallel.For)
        // Assuming health is an int or using a thread-safe wrapper
        Interlocked.Add(ref damageBuffer[enemyIdx], p.Damage);

        // 2. Reduce Penetration
        p.PenetrationCount--;
        p.HitCooldownFrames = 5;
        // 3. (Optional) Trigger a visual hit effect or sound index
        // Note: Don't instantiate nodes here! Just flag the enemy or add to a buffer.
    }
}
