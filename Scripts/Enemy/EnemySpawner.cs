/*

using CastleDefender.Scripts.Enemy.BehaviorMachine;
using CastleDefender.Scripts.World;
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class EnemySpawner : Node2D
{
    private Vector2 SpawnPosition = Vector2.Zero;
    private int Spawnlimit = 100;
    private int Budget;
    public Vector2 BiasDirection { get; set; }
    private Vector2 CenterPoint;
    private float _currentAngle, OrbitRadius, OrbitSpeed, RandomOffset, WobblePhase, WobbleSpeed;
    private PackedScene EnemyScene = GD.Load<PackedScene>("res://Scenes/Enemies/EnemyBody.tscn");
    private static readonly Dictionary<string, SpriteFrames> AnimationCache = new Dictionary<string, SpriteFrames>();
    private int EnemySpawnCount = 0;
    private Random rand = new Random();

    private readonly FieldHandler FieldHandler;

    private readonly Timer SpawnTimer = new Timer();
    private Timer MovementTimer = new Timer();
    private Sprite2D DebugIcon = new Sprite2D();

    public EnemySpawner(FieldHandler fieldHandler, float OrbitRadius, float OrbitSpeed, Vector2 CenterPoint, float BiasOffset, int Spawnlimit, int Budget, Random rand) : base()
    {

        this.Position = Vector2.Zero;
        this.FieldHandler = fieldHandler;
        this.OrbitRadius = OrbitRadius;
        this.OrbitSpeed = OrbitSpeed;
        this.Spawnlimit = Spawnlimit;
        this.Budget = Budget;
        this.rand = rand;
        this.CenterPoint = CenterPoint;
        float x = rand.Next(-1000, 1001) / 1000f;
        float y = rand.Next(-1000, 1001) / 1000f;
        SpawnPosition = (new Vector2(x, y).Normalized() * OrbitRadius) + CenterPoint;
        //DebugIcon.Position = SpawnPosition;
        _currentAngle = (SpawnPosition - CenterPoint).Angle();

        this.RandomOffset = (float)GD.RandRange(-BiasOffset, BiasOffset);
        WobblePhase = (float)GD.RandRange(0.5f, 2f);
        WobbleSpeed = (float)GD.RandRange(0.1f, 1f);

        //DebugIcon.Texture = GD.Load<Texture2D>("res://icon.svg");
        //base.AddChild(DebugIcon);

        this.SpawnTimer = new Timer();
        SpawnTimer.Autostart = true;
        SpawnTimer.WaitTime = GD.RandRange(0.1f, 1f);
        SpawnTimer.OneShot = false;
        SpawnTimer.Timeout += OnSpawnTimerTimeout;

        this.MovementTimer = new Timer();
        MovementTimer.Autostart = true;
        MovementTimer.WaitTime = 0.2f;
        MovementTimer.OneShot = false;
        MovementTimer.Timeout += OnMovementTimerTimerout;

        base.AddChild(SpawnTimer);
        base.AddChild(MovementTimer);
    }
    private static SpriteFrames GetAnimation(string AnimationPath)
    {
        if (!AnimationCache.ContainsKey(AnimationPath))
        {
            AnimationCache[AnimationPath] = GD.Load<SpriteFrames>(AnimationPath);
        }
        return AnimationCache[AnimationPath];
    }

    public void SubtractEnemy()
    {
        EnemySpawnCount--;
    }
    private float _timePassed;


    public void OnMovementTimerTimerout()
    {
        _timePassed += (float)MovementTimer.WaitTime;

        // 1. Move the Base Angle toward the Bias
        // This is independent of the wobble
        float targetBiasAngle = BiasDirection.Angle() + RandomOffset;
        _currentAngle = Mathf.LerpAngle(_currentAngle, targetBiasAngle, (float)MovementTimer.WaitTime * OrbitSpeed);

        // 2. Calculate the Wobble Offset
        // This is strictly a math function based on time, not lerping
        float currentWobble = Mathf.Sin(_timePassed * WobbleSpeed) * WobblePhase;

        // 3. Combine them for the final position
        float finalAngle = _currentAngle + currentWobble;

        Vector2 offset = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle)) * OrbitRadius;
        SpawnPosition = CenterPoint + offset;
        //DebugIcon.Position = SpawnPosition;
    }

    public void OnSpawnTimerTimeout()
    {
        if (EnemySpawnCount <= Spawnlimit && Budget > 0)
        {
            var anim = anims[rand.Next(0, 2)];
            var enemy = CreateEnemy(new EnemyStats(), anim);
            var offsetVector = new Vector2(rand.Next(0, 128), rand.Next(0, 128));
            enemy.Position = SpawnPosition + offsetVector;
            base.AddChild(enemy);
            EnemySpawnCount++;
            Budget--;
        }

        if (Budget == 0 && EnemySpawnCount == 0)
        {
            GD.Print("Spawner Finished");
            OnBudgetSpent();
        }
    }


    private void OnBudgetSpent()
    {
        base.QueueFree();
    }

    private EnemyBody CreateEnemy(EnemyStats enemyStats, string AnimationPath)
    {
        var enemy = (EnemyBody)EnemyScene.Instantiate();
        enemy.stats = EnemyStats.GetRandomizedStats(EnemyType.MELEE, rand);
        enemy.avoidanceLayer = rand.Next(1, 5);
        enemy.debug = false;
        enemy.FieldHandler = FieldHandler;
        var anim = GetAnimation(AnimationPath);
        enemy.sprite.SpriteFrames = anim;

        switch (enemyStats.EnemyType)
        {
            case EnemyType.MELEE:
                break;
            case EnemyType.RANGED:
                break;
            case EnemyType.ELITE:
                break;
            default:
                break;
        }

        return enemy;
    }
    private string[] anims = new string[]{
        "res://Scenes/Enemies/Enemy_Animations/Skeletons/Skeleton_2.tres",
        "res://Scenes/Enemies/Enemy_Animations/Skeletons/Skeleton_3.tres"
    };

}

*/