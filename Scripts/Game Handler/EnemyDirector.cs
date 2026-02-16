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
    private Timer TickTimer = new Timer();

    private static readonly Dictionary<string, SpriteFrames> AnimationCache = new Dictionary<string, SpriteFrames>();
    private Array<string> anims = new Array<string>(){
        "res://assets/Animations/Skeleton01.tres"
    };
    private readonly Dictionary<Rid, int> PhysicsRidToEnemy = new Dictionary<Rid, int>();
    private readonly Dictionary<Rid, int> AvoidanceRidToEnemy = new Dictionary<Rid, int>();

    private bool isProcessing = false;

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
        TickTimer.WaitTime = 0.05f;
        TickTimer.Timeout += OnTimerTimeout;

        base.AddChild(TickTimer);
    }

    public void OnTimerTimeout()
    {

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
            NavigationServer2D.AgentSetVelocity();
        }
    }
    private double Ticks = 0;
    public override void _PhysicsProcess(double delta)
    {
        Ticks += delta;
        if (Ticks < 0.1f)
            return;
        delta = Ticks;
        Ticks -= 0.1f;

        if (isProcessing)
            Processing((float)delta);
    }

    public override void _Process(double delta)
    {
        // Calculate how far we are through the current tick (0.0 to 1.0)
        float t = (float)Ticks / 0.1f;
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
        Vector2 motion = enemy.Velocity * delta;
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

        var agent = CreateAvoidanceEnemy(index, 1, 1, SpawnPosition + spawnOffset);
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

    public Rid CreateAvoidanceEnemy(int index, uint layer, uint mask, Vector2 spawnPos)
    {
        Rid agent = NavigationServer2D.AgentCreate();
        NavigationServer2D.AgentSetMap(agent, GetWorld2D().NavigationMap);
        NavigationServer2D.AgentSetPosition(agent, spawnPos);

        NavigationServer2D.AgentSetRadius(agent, 16.0f);
        NavigationServer2D.AgentSetNeighborDistance(agent, 100.0f);
        NavigationServer2D.AgentSetMaxNeighbors(agent, 10);

        NavigationServer2D.AgentSetAvoidanceLayers(agent, layer);
        NavigationServer2D.AgentSetAvoidanceMask(agent, mask);

        var callback = Callable.From((Vector2 safeVelocity) => OnSafeVelocityComputed(index, safeVelocity));
        NavigationServer2D.AgentSetAvoidanceCallback(agent, callback);
        return agent;
    }

    public void OnSafeVelocityComputed(int i, Vector2 safeVelocity)
    {
        _enemies[i].Velocity = safeVelocity;
    }
}