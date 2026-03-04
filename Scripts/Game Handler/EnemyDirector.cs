using System;
using System.Threading.Tasks;
using CastleDefender.Scripts.Enemy;
using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;


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
    private Dictionary<Rid, int> AvoidanceRidToEnemy = new Dictionary<Rid, int>();
private bool isProcessing = false;
    // timing for per-group updates – we no longer accumulate a 10 Hz "tick".
    // Each physics frame advances a single group; `groupDelta` for that group is
    // the incoming `delta` multiplied by the number of groups, reflecting the
    // full time between updates for members of that group.  We store an array of
    // these deltas and compute interpolation factors from them.
    private float[] lastGroupDelta;
    private float[] interpFactor;    // track how much real time has elapsed since each group was last processed
    private float[] elapsedSinceUpdate;
    private const int PhysicsGroupCount = 20; // still adjustable
    private int PhysicsGroupSize = 0;
    private int ActivePhysicsGroup = 0; private Vector2I[] lastGridCell; // for per-frame targeting updates

    // debug monitor: track rolling average of physics process time
#if DEBUG
    private float[] physicsFrameTimes;
    private int physicsFrameIndex = 0;
    private const int PhysicsFrameHistorySize = 60;
#endif

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

#if DEBUG
        // initialize monitor for rolling average of physics frame time
        physicsFrameTimes = new float[PhysicsFrameHistorySize];
        System.Array.Fill(physicsFrameTimes, 0f);
        Performance.AddCustomMonitor("EnemyDirector/PhysicsAvgMs", Callable.From(GetPhysicsAverageMs));
#endif
    }

#if DEBUG
    private float GetPhysicsAverageMs()
    {
        float sum = 0f;
        foreach (var t in physicsFrameTimes)
            sum += t;
        return sum / PhysicsFrameHistorySize;
    }
#endif

    public EnemyData GetEnemyAt(int index)
    {
        return _enemies[index];
    }

    public void DamageEnemy(Rid rid, int Damage)
    {
        _enemies[AvoidanceRidToEnemy[rid]].takeDamage(Damage);
    }

    public void DamageEnemy(int index, int Damage)
    {
        _enemies[index].takeDamage(Damage);
    }

    public void DamageEnemiesInArea(Vector2 center, float radius, int damage)
    {
        // Query spatial grid cells within radius of center point and damage all enemies
        // within the radius from the center
        Vector2I centerCell = fieldHandler.LocalToMap(center);
        
        // Estimate search radius in grid cells (SpatialEntityGrid uses ~50 unit cells)
        float cellSize = 50f;
        int searchRadius = Mathf.CeilToInt(radius / cellSize) + 1;
        
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                Vector2I cell = centerCell + new Vector2I(dx, dy);
                int enemyIdx = entityGrid.GetFirstInCell(cell);
                
                while (enemyIdx != -1)
                {
                    if (_enemies[enemyIdx].isAlive)
                    {
                        float distToCenter = _enemies[enemyIdx].Position.DistanceTo(center);
                        if (distToCenter <= radius)
                        {
                            _enemies[enemyIdx].takeDamage(damage);
                        }
                    }
                    enemyIdx = entityGrid.GetNext(enemyIdx);
                }
            }
        }
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
        lastGridCell = new Vector2I[EnemyMaximum];
        AvoidanceRidToEnemy.Clear();
        PhysicsGroupSize = (int)(EnemyMaximum / PhysicsGroupCount);

        // ensure our timing arrays are sized correctly whenever the enemy count
        // or group count changes.
        lastGroupDelta = new float[PhysicsGroupCount];
        interpFactor = new float[PhysicsGroupCount];
        elapsedSinceUpdate = new float[PhysicsGroupCount];
        for (int g = 0; g < PhysicsGroupCount; g++)
        {
            // give a reasonable default so early frames don't divide by zero
            lastGroupDelta[g] = 1f / 60f * PhysicsGroupCount;
            interpFactor[g] = 1f;
            elapsedSinceUpdate[g] = lastGroupDelta[g];
        }

        for (int i = 0; i < EnemyMaximum; i++)
        {
            uint physicsIndex = (uint)(i / PhysicsGroupSize);
            if (physicsIndex >= PhysicsGroupCount)
                physicsIndex = PhysicsGroupCount - 1;

            _enemies[i] = new EnemyData(physicsIndex);
            lastGridCell[i] = new Vector2I(-1, -1);
        }
    }

    private ParallelOptions _parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = Mathf.Max(1, System.Environment.ProcessorCount / 2)
    };

    public void Processing(int start, int end, float delta)
    {
        for (int i = start; i < end; i++)
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

        }
        Parallel.For(start, end, _parallelOptions, i =>
        {
            if (!_enemies[i].isAlive)
                return;

            EnemyLogic.HandleBasicEnemy(ref _enemies[i]);

            // remember the visual "last" position before any substeps so
            // interpolation in _Process uses the pre-update position
            _enemies[i].LastPosition = _enemies[i].Position;
            // === PER-ENEMY SUBSTEP LOGIC ===
            // Compute required substeps from the distance this enemy will travel
            // in `delta`. This prevents over-splitting for slow enemies while
            // ensuring fast enemies don't tunnel through walls.
            float maxMovePerSubstep = 8f; // world units; adjust (tile size = 8)
            float totalMove = _enemies[i].Speed * delta;

            int numSubsteps = Mathf.CeilToInt(totalMove / maxMovePerSubstep);
            if (numSubsteps < 1)
                numSubsteps = 1;
            const int maxSubstepsCap = 32; // safety cap to avoid pathological CPU spikes
            if (numSubsteps > maxSubstepsCap)
            {
                numSubsteps = maxSubstepsCap;
                // optional debug: GD.Print($"Enemy {i}: clamped numSubsteps to {numSubsteps}");
            }

            float substepDelta = delta / numSubsteps;

            for (int step = 0; step < numSubsteps; step++)
            {
                ApplyFieldPhysics(ref _enemies[i], substepDelta);
            }
            // === END PER-ENEMY SUBSTEP LOGIC ===

            NavigationServer2D.AgentSetPosition(_enemies[i].Agent, _enemies[i].Position);
            NavigationServer2D.AgentSetVelocity(_enemies[i].Agent, _enemies[i].Velocity);
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isProcessing)
            return;

#if DEBUG
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

        // every physics frame we process exactly one group.  Because each enemy
        // only gets updated every `PhysicsGroupCount` frames, we want the delta
        // passed to its physics logic to reflect the *total* elapsed time since
        // its previous update.  Multiplying `delta` by the group count achieves
        // that while keeping `Speed` meaningfully in units per second.
        float groupDelta = (float)delta * PhysicsGroupCount;
        lastGroupDelta[ActivePhysicsGroup] = groupDelta;
        // reset the elapsed timer for this group so interpolation can start anew
        elapsedSinceUpdate[ActivePhysicsGroup] = 0f;

        var startIndex = PhysicsGroupSize * ActivePhysicsGroup;
        var endIndex = PhysicsGroupSize * (ActivePhysicsGroup + 1);
        if (ActivePhysicsGroup == PhysicsGroupCount - 1)
            endIndex = (int)EnemyMaximum;

        Processing(startIndex, endIndex, groupDelta);
        ActivePhysicsGroup = (ActivePhysicsGroup + 1) % PhysicsGroupCount;

#if DEBUG
        stopwatch.Stop();
        physicsFrameTimes[physicsFrameIndex] = (float)stopwatch.Elapsed.TotalMilliseconds;
        physicsFrameIndex = (physicsFrameIndex + 1) % PhysicsFrameHistorySize;
#endif
    }

    public override void _Process(double delta)
    {
        if (!isProcessing)
            return;

        // update elapsed time for each group first
        for (int g = 0; g < PhysicsGroupCount; g++)
        {
            elapsedSinceUpdate[g] += (float)delta;
            float ld = lastGroupDelta[g];
            // interpolation ratio progresses linearly from 0 to 1 over the period
            // between group updates.  this avoids the asymptotic behaviour of using
            // only the single-frame delta.
            interpFactor[g] = (ld > 0f) ? Mathf.Clamp(elapsedSinceUpdate[g] / ld, 0f, 1f) : 0f;
        }

        for (int i = 0; i < EnemyMaximum; i++)
        {
            if (!_enemies[i].isAlive)
                continue;
            ref var enemy = ref _enemies[i];
            var target = enemy.Position;

            float t = interpFactor[enemy.PhysicsGroup];
            Vector2 smoothedPos = enemy.LastPosition.Lerp(target, t);

            // Apply to visual only
            enemy.sprite.Position = smoothedPos;
            // update grid cell based on interpolated position, helps towers fire
            var newCell = fieldHandler.LocalToMap(smoothedPos);
            var oldCell = lastGridCell[i];
            if (newCell != oldCell)
            {
                if (oldCell.X >= 0)
                {
                    entityGrid.MoveEntity(i, oldCell, newCell);
                    fieldHandler.ReduceDensityAt(oldCell);
                }
                fieldHandler.IncreaseDensityAt(newCell);
                lastGridCell[i] = newCell;
            }
        }
    }

    public void ApplyFieldPhysics(ref EnemyData enemy, float delta)
    {
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
        // Blend field velocity with avoidance velocity for smooth transitions.
        // If SafeVelocity is zero, use field velocity only; otherwise blend them
        // so avoidance guides movement without jarring switches.
        Vector2 chosenVel = enemy.Velocity;
        if (enemy.SafeVelocity != Vector2.Zero)
        {
            // 50% field/target velocity, 50% avoidance guidance.
            // Higher ratio (e.g. 0.5 vs 0.2) means RVO has more say, useful for
            // enforcing crowding behavior in narrow passages.
            chosenVel = enemy.Velocity.Lerp(enemy.SafeVelocity, 0.5f);
        }
        Vector2 motion = chosenVel * delta;

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
        if (_enemies[index].sprite.GetParent() != this)
            base.AddChild(_enemies[index].sprite);



        var agent = CreateAvoidanceEnemy(index, 1, pos);
        AvoidanceRidToEnemy[agent] = index;

        var cellPos = fieldHandler.LocalToMap(pos);
        fieldHandler.IncreaseDensityAt(cellPos);
        entityGrid.AddToCell(index, cellPos);
        lastGridCell[index] = cellPos;
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