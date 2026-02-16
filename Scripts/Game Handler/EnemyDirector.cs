using System;
using System.Threading.Tasks;
using CastleDefender.Scripts.Enemy.BehaviorMachine;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;


public partial class EnemyDirector : Node2D
{
    private uint _maximum;
    public uint EnemyMaximum { get => _maximum; set { _maximum = value; Instantiate(); } }
    public uint Budget = 100;
    public Vector2 SpawnPosition = new Vector2(512, 512);
    private EnemyData[] _enemies;

    private static readonly Dictionary<string, SpriteFrames> AnimationCache = new Dictionary<string, SpriteFrames>();
    private Array<string> anims = new Array<string>(){
        "res://assets/Animations/Skeleton01.tres",
        "res://assets/Animations/Skeleton02.tres",
        "res://assets/Animations/Skeleton03.tres",
    };
    private readonly Dictionary<Rid, int> PhysicsRidToEnemy = new Dictionary<Rid, int>();
    private readonly Dictionary<Rid, int> AvoidanceRidToEnemy = new Dictionary<Rid, int>();

    private bool isProcessing = false;
    private Rid NavigationMap = NavigationServer2D.MapCreate();
    private static SpriteFrames GetAnimation(string AnimationPath)
    {
        if (!AnimationCache.ContainsKey(AnimationPath))
        {
            AnimationCache[AnimationPath] = GD.Load<SpriteFrames>(AnimationPath);
        }
        return AnimationCache[AnimationPath];
    }

    public override void _Ready()
    {
        NavigationServer2D.MapSetActive(NavigationMap, true);
        NavigationServer2D.MapSetCellSize(NavigationMap, 8f);

        base.YSortEnabled = true;
    }

    public void StartProcessing()
    {
        isProcessing = true;
    }

    public void StopProcessing()
    {
        isProcessing = false;
    }

    private void Instantiate()
    {
        _enemies = new EnemyData[EnemyMaximum];
        PhysicsRidToEnemy.Clear();
        AvoidanceRidToEnemy.Clear();
        for (int i = 0; i < EnemyMaximum; i++)
            _enemies[i] = new EnemyData();
    }

    public void DamageEnemy(Rid PhysicsRid, int Damage)
    {
        var index = PhysicsRidToEnemy[PhysicsRid];
        _enemies[index].takeDamage(Damage);
    }

    public void Processing(float delta)
    {
        for (int i = 0; i < EnemyMaximum; i++)
        {
            if (Budget > 0 && _enemies[i].isDead())
            {
                ReviveEnemyAt(i);
                continue;
            }

            EnemyLogic.HandleBasicEnemy(ref _enemies[i]);

            ApplyPhysics(ref _enemies[i], delta);

            NavigationServer2D.AgentSetPosition(_enemies[i].Agent, _enemies[i].Position);
            NavigationServer2D.AgentSetVelocity(_enemies[i].Agent, _enemies[i].Velocity);
        }
    }
    private double Ticks = 0;
    private double SecondsPerTick = 0.2f;
    public override void _PhysicsProcess(double delta)
    {
        if (!isProcessing)
            return;


        Ticks += delta;
        if (Ticks < SecondsPerTick)
            return;
        delta = Ticks;
        Ticks -= SecondsPerTick;

        Processing((float)delta);
    }

    public override void _Process(double delta)
    {
        // Calculate how far we are through the current tick (0.0 to 1.0)
        if (!isProcessing)
            return;
        float t = (float)Ticks / (float)SecondsPerTick;
        for (int i = 0; i < EnemyMaximum; i++)
        {
            ref var enemy = ref _enemies[i];
            var target = enemy.Position;
            // Linear Interpolation (Lerp)
            Vector2 smoothedPos = enemy.LastPosition.Lerp(target, t);

            // Apply to visual only
            enemy.sprite.Position = smoothedPos;

        }
    }


    public void ApplyPhysics(ref EnemyData enemy, float delta)
    {
        enemy.LastPosition = enemy.Position;
        Vector2 motion = enemy.SafeVelocity * delta;
        if (enemy.SafeVelocity == Vector2.Zero)
            motion = enemy.Velocity * delta;

        Transform2D currentTransform = new Transform2D(0, enemy.Position);

        var params2d = new PhysicsTestMotionParameters2D();
        params2d.From = currentTransform;
        params2d.Motion = motion;
        params2d.Margin = 0.08f;
        params2d.RecoveryAsCollision = true; // Crucial for not getting stuck in walls

        var result = new PhysicsTestMotionResult2D();

        if (PhysicsServer2D.BodyTestMotion(enemy.PhysicsBody, params2d, result))
        {
            // 1. Move the distance that was actually safe to travel
            enemy.Position += result.GetTravel();

            // 2. Calculate the slide for the remaining motion
            Vector2 remainder = result.GetRemainder();
            Vector2 normal = result.GetCollisionNormal();
            Vector2 slideMotion = remainder.Slide(normal);

            // 3. Optional: Test the slide motion too (for double-wall corners)
            // For 2,000 enemies, you might skip a second test and just apply slideMotion
            enemy.Position += slideMotion;
        }
        else
        {
            // No collision, move the full intended distance
            enemy.Position += motion;
        }

        // Update the Server so the "Ghost" body stays in sync
        PhysicsServer2D.BodySetState(enemy.PhysicsBody, PhysicsServer2D.BodyState.Transform, new Transform2D(0, enemy.Position));
    }



    private void ReviveEnemyAt(int index)
    {
        var spawnOffset = new Vector2(GD.Randf() * 512, GD.Randf() * 512);
        _enemies[index].ReviveEnemy(SpawnPosition + spawnOffset, Vector2.Zero, 100, GetAnimation(anims.PickRandom()));
        base.AddChild(_enemies[index].sprite);
        _enemies[index].sprite.Position = SpawnPosition + spawnOffset;

        var body = CreatePhysicsEnemy(SpawnPosition + spawnOffset, 8);
        PhysicsRidToEnemy[body] = index;
        _enemies[index].PhysicsBody = body;

        var agent = CreateAvoidanceEnemy(index, 3, SpawnPosition + spawnOffset);
        _enemies[index].Agent = agent;
        AvoidanceRidToEnemy[agent] = index;

    }
    public Rid CreatePhysicsEnemy(Vector2 spawnPos, float radius)
    {
        Rid body = PhysicsServer2D.BodyCreate();
        PhysicsServer2D.BodySetMode(body, PhysicsServer2D.BodyMode.Kinematic);
        Rid shape = PhysicsServer2D.CircleShapeCreate();

        PhysicsServer2D.ShapeSetData(shape, radius);
        PhysicsServer2D.BodyAddShape(body, shape);
        PhysicsServer2D.BodySetCollisionLayer(body, 2);
        PhysicsServer2D.BodySetCollisionMask(body, 1);

        Rid space = GetWorld2D().Space;
        PhysicsServer2D.BodySetSpace(body, space);

        var transform = new Transform2D(0, spawnPos);
        PhysicsServer2D.BodySetState(body, PhysicsServer2D.BodyState.Transform, transform);

        return body;
    }

    public Rid CreateAvoidanceEnemy(int index, uint layerCount, Vector2 spawnPos)
    {
        Rid agent = NavigationServer2D.AgentCreate();

        NavigationServer2D.AgentSetMap(agent, GetWorld2D().NavigationMap);
        NavigationServer2D.AgentSetPosition(agent, spawnPos);

        NavigationServer2D.AgentSetRadius(agent, 5.0f);
        NavigationServer2D.AgentSetNeighborDistance(agent, 50.0f);
        NavigationServer2D.AgentSetMaxNeighbors(agent, 5);
        NavigationServer2D.AgentSetMaxSpeed(agent, 100f);
        NavigationServer2D.AgentSetTimeHorizonAgents(agent, 0.5f);

        uint rand = (uint)GD.RandRange(1, layerCount);
        NavigationServer2D.AgentSetAvoidanceLayers(agent, rand);
        NavigationServer2D.AgentSetAvoidanceMask(agent, rand);
        NavigationServer2D.AgentSetAvoidanceEnabled(agent, true);

        var callback = Callable.From((Vector2 safeVelocity) => OnSafeVelocityComputed(index, safeVelocity));
        NavigationServer2D.AgentSetAvoidanceCallback(agent, callback);
        return agent;
    }

    public void OnSafeVelocityComputed(int i, Vector2 safeVelocity)
    {
        _enemies[i].SafeVelocity = safeVelocity;

    }
}