using System;
using System.Threading.Tasks;
using CastleDefender.Scripts.Enemy;
using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;


public partial class EnemyDirector : Node2D
{
    private MapHandler mapHandler;
    private FieldHandler fieldHandler;
    private SpatialEntityGrid entityGrid;
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
    private Dictionary<Rid, int> PhysicsRidToEnemy = new Dictionary<Rid, int>();
    private Dictionary<Rid, int> AvoidanceRidToEnemy = new Dictionary<Rid, int>();
    public ref EnemyData GetEnemyFromRid(Rid rid)
    {
        return ref _enemies[PhysicsRidToEnemy[rid]];
    }
    private bool isProcessing = false;
    private double Ticks = 0;
    private double SecondsPerTick = 0.1f;
    private const int PhysicsGroupCount = 6;
    private int PhysicsGroupSize = 0;
    private int ActivePhysicsGroup = 0;

    private Rid NavigationMap = NavigationServer2D.MapCreate();
    private static SpriteFrames GetAnimation(string AnimationPath)
    {
        if (!AnimationCache.ContainsKey(AnimationPath))
        {
            AnimationCache[AnimationPath] = GD.Load<SpriteFrames>(AnimationPath);
        }
        return AnimationCache[AnimationPath];
    }

    public EnemyDirector(MapHandler mapHandler, FieldHandler fieldHandler, SpatialEntityGrid entityGrid) : base()
    {
        this.mapHandler = mapHandler;
        this.fieldHandler = fieldHandler;
        this.entityGrid = entityGrid;
    }

    public override void _Ready()
    {
        NavigationServer2D.MapSetActive(NavigationMap, true);
        NavigationServer2D.MapSetCellSize(NavigationMap, 8f);

        base.YSortEnabled = true;
    }

    public EnemyData GetEnemyAt(int index)
    {
        return _enemies[index];
    }

    public void DamageEnemy(Rid rid, int Damage)
    {
        _enemies[PhysicsRidToEnemy[rid]].takeDamage(Damage);
    }

    public void DamageEnemy(int index, int Damage)
    {
        _enemies[index].takeDamage(Damage);
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

        PhysicsGroupSize = (int)(EnemyMaximum / PhysicsGroupCount);


    }
    private ParallelOptions _parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = Mathf.Max(1, System.Environment.ProcessorCount / 2)
    };
    public void Processing(float delta)
    {
        for (int i = 0; i < EnemyMaximum; i++)
        {
            if (Budget > 0 && !_enemies[i].isAlive)
            {
                ReviveEnemyAt((int)i);
                Budget -= 1;
            }
            if (_enemies[i].pendingDead)
            {
                _enemies[i].Kill();
                var pos = fieldHandler.LocalToMap(_enemies[i].Position);
                fieldHandler.ReduceDensityAt(pos);
                entityGrid.RemoveFromCell(i, pos);
                continue;
            }
            if (!_enemies[i].isAlive)
                continue;

            HandleEnemyAnimation(i);
            HandleEnemyCellTransition(i);
        }
        Parallel.For(0, EnemyMaximum, _parallelOptions, i =>
        {
            if (!_enemies[i].isAlive)
                return;

            EnemyLogic.HandleBasicEnemy(ref _enemies[i]);

            ApplyFieldPhysics(ref _enemies[i], delta);

            NavigationServer2D.AgentSetPosition(_enemies[i].Agent, _enemies[i].Position);
            NavigationServer2D.AgentSetVelocity(_enemies[i].Agent, _enemies[i].Velocity);
        });

    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isProcessing)
            return;
        UpdatePhysicsBatch();
        ActivePhysicsGroup = (ActivePhysicsGroup + 1) % PhysicsGroupCount;

        Ticks += delta;
        if (Ticks < SecondsPerTick)
            return;
        delta = Ticks;
        Ticks -= SecondsPerTick;

        Processing((float)delta);
    }

    public void UpdatePhysicsBatch()
    {
        var startIndex = PhysicsGroupSize * ActivePhysicsGroup;
        var endIndex = PhysicsGroupSize * (ActivePhysicsGroup + 1);
        if (ActivePhysicsGroup == PhysicsGroupCount - 1)
            endIndex = (int)EnemyMaximum;
        for (int i = startIndex; i < endIndex; i++)
        {
            if (!_enemies[i].isAlive)
                continue;
            PhysicsServer2D.BodySetState(_enemies[i].PhysicsBody, PhysicsServer2D.BodyState.Transform, new Transform2D(0, _enemies[i].Position));

        }
    }

    public override void _Process(double delta)
    {
        // Calculate how far we are through the current tick (0.0 to 1.0)
        if (!isProcessing)
            return;
        float t = (float)Ticks / (float)SecondsPerTick;
        for (int i = 0; i < EnemyMaximum; i++)
        {
            if (!_enemies[i].isAlive)
                continue;
            ref var enemy = ref _enemies[i];
            var target = enemy.Position;
            // Linear Interpolation (Lerp)
            Vector2 smoothedPos = enemy.LastPosition.Lerp(target, t);

            // Apply to visual only
            enemy.sprite.Position = smoothedPos;

        }
    }

    public void ApplyFieldPhysics(ref EnemyData enemy, float delta)
    {
        enemy.LastPosition = enemy.Position;

        // 1. Get the Field Data for current position
        Vector2 fieldVector = fieldHandler.GetDirectionAt(enemy.Position);

        // 2. STUCK CHECK: If we are already inside a wall
        if (mapHandler.IsWallAt(enemy.Position))
        {
            // Use the wall's own vector to push the enemy out.
            // We multiply by a 'Panic Factor' to ensure they eject quickly.
            float ejectSpeed = 200.0f;
            enemy.Position += fieldVector * ejectSpeed * delta;
            return; // Exit early; getting out is the priority
        }

        // 3. NORMAL MOVEMENT: Calculate intended move
        Vector2 motion = enemy.SafeVelocity * delta;
        if (enemy.SafeVelocity == Vector2.Zero)
            motion = enemy.Velocity * delta;

        Vector2 nextPos = enemy.Position + motion;

        // 4. LOOK-AHEAD CHECK: Is the destination a wall?
        if (!mapHandler.IsWallAt(nextPos))
        {
            enemy.Position = nextPos;
        }
        else
        {
            // 5. SLIDING: If the destination is a wall, try to 'skim' it
            // Check X and Y movement separately (The Sliding logic)
            Vector2 xMove = enemy.Position + new Vector2(motion.X, 0);
            if (!mapHandler.IsWallAt(xMove))
            {
                enemy.Position = xMove;
            }
            else
            {
                Vector2 yMove = enemy.Position + new Vector2(0, motion.Y);
                if (!mapHandler.IsWallAt(yMove))
                {
                    enemy.Position = yMove;
                }
            }
        }
    }



    private void ReviveEnemyAt(int index)
    {
        var spawnOffset = new Vector2(GD.Randf() * 512, GD.Randf() * 512);
        var pos = SpawnPosition + spawnOffset;
        _enemies[index].ReviveEnemy(pos, Vector2.Zero, GD.RandRange(75, 125), GetAnimation(anims.PickRandom()));
        if (_enemies[index].PhysicsBody.Id == 0)
            base.AddChild(_enemies[index].sprite);


        var body = CreatePhysicsEnemy(pos, 8, index);
        PhysicsRidToEnemy[body] = index;

        var agent = CreateAvoidanceEnemy(index, 1, pos);
        AvoidanceRidToEnemy[agent] = index;

        var cellPos = fieldHandler.LocalToMap(pos);
        fieldHandler.IncreaseDensityAt(cellPos);
        entityGrid.AddToCell(index, cellPos);
    }
    public Rid CreatePhysicsEnemy(Vector2 spawnPos, float radius, int index)
    {
        Rid body = new Rid();
        if (_enemies[index].PhysicsBody.Id == 0)
        {
            body = PhysicsServer2D.BodyCreate();
            PhysicsServer2D.BodySetMode(body, PhysicsServer2D.BodyMode.Kinematic);
            Rid shape = PhysicsServer2D.CircleShapeCreate();

            PhysicsServer2D.ShapeSetData(shape, radius);
            PhysicsServer2D.BodyAddShape(body, shape);
            PhysicsServer2D.BodySetCollisionLayer(body, 2);
            PhysicsServer2D.BodySetCollisionMask(body, 1);

            Rid space = GetWorld2D().Space;
            PhysicsServer2D.BodySetSpace(body, space);
            _enemies[index].PhysicsBody = body;
            _enemies[index].PhysicsShape = shape;
        }
        else
        {
            body = _enemies[index].PhysicsBody;
            PhysicsServer2D.ShapeSetData(_enemies[index].PhysicsShape, radius);
        }



        var transform = new Transform2D(0, spawnPos);
        PhysicsServer2D.BodySetState(body, PhysicsServer2D.BodyState.Transform, transform);

        return body;
    }

    public Rid CreateAvoidanceEnemy(int index, uint layerCount, Vector2 spawnPos)
    {
        Rid agent = new Rid();
        if (_enemies[index].Agent.Id == 0)
        {
            agent = NavigationServer2D.AgentCreate();
            NavigationServer2D.AgentSetMap(agent, GetWorld2D().NavigationMap);
            NavigationServer2D.AgentSetRadius(agent, 5.0f);
            NavigationServer2D.AgentSetNeighborDistance(agent, 50.0f);
            NavigationServer2D.AgentSetMaxNeighbors(agent, 5);
            NavigationServer2D.AgentSetMaxSpeed(agent, 100f);
            NavigationServer2D.AgentSetTimeHorizonAgents(agent, 0.5f);

            uint rand = (uint)GD.RandRange(1, layerCount);
            _enemies[index].layer = rand;
            NavigationServer2D.AgentSetAvoidanceLayers(agent, rand);
            NavigationServer2D.AgentSetAvoidanceMask(agent, rand);
            NavigationServer2D.AgentSetAvoidanceEnabled(agent, true);

            var callback = Callable.From((Vector2 safeVelocity) => OnSafeVelocityComputed(index, safeVelocity));
            NavigationServer2D.AgentSetAvoidanceCallback(agent, callback);
            _enemies[index].Agent = agent;
        }
        else
        {
            agent = _enemies[index].Agent;
        }



        NavigationServer2D.AgentSetPosition(agent, spawnPos);
        return agent;
    }

    public void OnSafeVelocityComputed(int i, Vector2 safeVelocity)
    {
        _enemies[i].SafeVelocity = safeVelocity;

    }

    private void HandleEnemyCellTransition(int index)
    {
        var pos = fieldHandler.LocalToMap(_enemies[index].Position);
        var lastPos = fieldHandler.LocalToMap(_enemies[index].LastPosition);
        if (pos == lastPos)
            return;
        fieldHandler.HandleCellTransition(pos, lastPos);
        entityGrid.MoveEntity(index, lastPos, pos);
    }

    private void HandleEnemyAnimation(int index)
    {
        var anim = GetAnimationPrefix(_enemies[index].CurrentState) + GetAnimationDirection(_enemies[index].Velocity);
        if (anim != _enemies[index].sprite.Animation)
        {
            _enemies[index].sprite.Animation = anim;
            _enemies[index].sprite.Play();
        }
    }
    private static string GetAnimationDirection(Vector2 Direction)
    {
        if (Direction == Vector2.Zero)
            return "_Down";
        if (Math.Abs(Direction.X) > Math.Abs(Direction.Y))
            return Direction.X > 0 ? "_Right" : "_Left";
        else
            return Direction.Y > 0 ? "_Down" : "_Up";
    }

    private static string GetAnimationPrefix(EnemyStateType state)
    {
        return AnimationPrefixes[state];
    }

    private static Dictionary<EnemyStateType, string> AnimationPrefixes = new Dictionary<EnemyStateType, string>()
    {
        {EnemyStateType.Idle,"_Idle"},
        {EnemyStateType.Attack,"_Attack"},
        {EnemyStateType.TargetRun,"_Run"},
        {EnemyStateType.Run,"_Run"},
        {EnemyStateType.Hurt,"_Hurt"},
        {EnemyStateType.Death,"_Death"}
    };
}