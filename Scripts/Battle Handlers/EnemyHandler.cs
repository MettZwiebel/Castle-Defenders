using CastleDefender.Scripts.World;
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

[GlobalClass]
public partial class EnemyHandler : Node
{
    public Vector2 AttackBias { get; set; } = Vector2.Zero;
    private readonly FieldHandler FieldHandler;
    private float SphereRadius;
    private Timer Timer = new Timer();
    private Node SpawnerList = new Node();
    private Random rand = new Random();

    [Signal]
    public delegate void OnBudgetSpentEventHandler();
    [Signal]
    public delegate void OnEliteIncomingEventHandler(int remainingDays);


    public EnemyHandler(FieldHandler fieldHandler, float SphereRadius) : base()
    {
        this.FieldHandler = fieldHandler;
        this.SphereRadius = SphereRadius;
        Timer.WaitTime = 1f;
        Timer.OneShot = false;
        Timer.Autostart = false;
        Timer.Timeout += OnTimerTimeout;

        base.AddChild(Timer);
        base.AddChild(SpawnerList);
    }

    public override void _Ready()
    {
        GD.Print("Enemy Handler Ready");
    }

    public void StartBattle(int Budget)
    {
        //uint count = 3 + (day / 5);
        uint count = 10;
        int budgetPerSpawner = (int)(Budget / count);
        GD.Print("Spawner Count: " + count + " | Budget per Spawner: " + budgetPerSpawner);
        for (int i = 0; i < count; i++)
        {
            EnemySpawner spawner = new EnemySpawner(FieldHandler, SphereRadius + 300f, 1, FieldHandler.MapToLocal(new Vector2I(99, 99)), 1, 100, budgetPerSpawner, new System.Random());
            SpawnerList.AddChild(spawner);
        }
        Timer.Start();
    }

    public void OnTimerTimeout()
    {
        if (SpawnerList.GetChildCount() == 0)
            SpawnerFinished();
        if (rand.Next(0, 10) < 1)
        {
            var vec = new Vector2(GD.RandRange(-1,1), GD.RandRange(-1,1));
            foreach (var child in SpawnerList.GetChildren())
            {
                if (child is EnemySpawner)
                {
                    var spawner = (EnemySpawner)child;
                    spawner.BiasDirection = vec.Normalized();
                }
            }
        }
    }


    private void SpawnerFinished()
    {

        Timer.Stop();
        GD.Print("EnemyHandler: Budget Spent");
        EmitSignal(SignalName.OnBudgetSpent);
    }


}
