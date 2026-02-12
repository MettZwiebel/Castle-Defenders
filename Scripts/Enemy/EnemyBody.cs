using Godot;
using CastleDefender.Scripts.World;
using CastleDefender.Scripts.Enemy.BehaviorMachine;
using Godot.Collections;
using System;

public partial class EnemyBody : CharacterBody2D
{
    public EnemyStats stats = new EnemyStats();
    public bool debug = true;

    public Label debugLabel = new Label();

    public NavigationAgent2D agent;
    public AnimatedSprite2D sprite { get; set; } = new AnimatedSprite2D();
    public EnemyStateMachine stateMachine = new EnemyStateMachine(EnemyStateMachine.Templates["basic"]);
    public Vector2 unqiueTarget;
    public TargetArea attackTarget;
    private Vector2I lastCellCoords = Vector2I.Zero;
    public EnemySpawner spawner;
    public int avoidanceLayer = 1;

    private string _anim;
    public string AnimationPrefix { get => _anim; set { _anim = value; SetAnimation(new Vector2I(0, 0)); } }
    public FieldHandler FieldHandler;
    private Dictionary<Vector2I, string> DirectionMapping = new Dictionary<Vector2I, string>
    {
            {new Vector2I(0,0),"_Down"},
            {new Vector2I(0,1),"_Down"},
            {new Vector2I(1,1),"_Right"},
            {new Vector2I(1,0),"_Right"},
            {new Vector2I(1,-1),"_Right"},
            {new Vector2I(0,-1),"_Up"},
            {new Vector2I(-1,-1),"_Left"},
            {new Vector2I(-1,0),"_Left"},
            {new Vector2I(-1,1),"_Left"}
        };
    public override void _Ready()
    {
        debugLabel.Visible = debug;
        spawner = (EnemySpawner)GetParent();
        stateMachine.Parent = this;

        foreach (var child in GetChildren())
        {

            if (child is NavigationAgent2D)
            {
                agent = (NavigationAgent2D)child;
                continue;
            }

        }

        agent.SetAvoidanceLayerValue(avoidanceLayer, true);
        agent.SetAvoidanceMaskValue(avoidanceLayer, true);

        sprite.Connect("animation_finished", Callable.From(stateMachine.OnAnimationFinished));
        base.AddChild(sprite);
        if (debug)
            base.AddChild(debugLabel);
        sprite.Play();
        lastCellCoords = FieldHandler.LocalToMap(Position);
        FieldHandler.EnterEnemyAt(lastCellCoords, this);
    }

    public void takeDamage(int damage)
    {
        stats.Health -= damage;
        if (stateMachine.CurrentState == EnemyStateType.Hurt || stateMachine.CurrentState == EnemyStateType.Death)
            return;
        if (stats.Health <= 0)
        {
            stateMachine.setNewState(EnemyStateType.Death);
        }
        else
        {
            stateMachine.setNewState(EnemyStateType.Hurt);
        }
    }
    private double StuckMethodThreshold = 2;
    private double stuckCounter = 0;
    private bool isStuck = false;
    private int counter = 0;
    public override void _PhysicsProcess(double delta)
    {
        stateMachine.ProcessPhysics();
        if (debug)
            debugLabel.Text = stateMachine.CurrentState.ToString();

        var mapCoords = FieldHandler.LocalToMap(Position);
        if (counter++ == 10)
        {
            SetAnimation(mapCoords);
            counter = 0;
        }

        AntiStuckMethod(delta, mapCoords);

        HandleCellTransition(mapCoords);


        MoveAndSlide();
    }

    private void AntiStuckMethod(double delta, Vector2I mapCoords)
    {
        if (stuckCounter >= StuckMethodThreshold)
            isStuck = true;
        if (!isStuck)
        {
            if (lastCellCoords == FieldHandler.LocalToMap(Position))
                stuckCounter += delta;
            else
                stuckCounter = 0;
        }
        else
        {
            if (lastCellCoords != FieldHandler.LocalToMap(Position))
            {
                agent.SetAvoidanceMaskValue(avoidanceLayer, true);
                SetCollisionMaskValue(1, true);
                isStuck = false;
            }
            else
            {
                agent.SetAvoidanceMaskValue(avoidanceLayer, false);
                SetCollisionMaskValue(1, false);
            }
        }
    }

    public void SetAnimation(Vector2I mapCoords)
    {
        var dir = FieldHandler.GetDirectionAt(mapCoords);
        Vector2I mappedDir = new Vector2I((int)Math.Round(dir.X), (int)Math.Round(dir.Y));
        sprite.Animation = AnimationPrefix + DirectionMapping[mappedDir];
    }

    private void HandleCellTransition(Vector2I mapCoords)
    {
        if (mapCoords != lastCellCoords)
        {
            //GD.Print("new Location: " + mapCoords);
            FieldHandler.ExitEnemyAt(lastCellCoords);

            FieldHandler.EnterEnemyAt(mapCoords, this);

            lastCellCoords = mapCoords;
        }
        var density = FieldHandler.GetDensityAt(mapCoords);
        if (density > 5)
        {
            agent.SetAvoidanceMaskValue(avoidanceLayer, false);
            SetCollisionMaskValue(1, false);
        }
        else if (density < 2)
        {
            SetCollisionMaskValue(1, true);
            agent.SetAvoidanceMaskValue(avoidanceLayer, true);
        }

    }

    public void Kill()
    {
        attackTarget?.freeEnemy(this);
        var mapCoords = FieldHandler.LocalToMap(Position);
        FieldHandler.ExitEnemyAt(mapCoords);
        spawner.SubtractEnemy();
        QueueFree();
    }

    public void setNewState(EnemyStateType newState)
    {
        stateMachine.setNewState(newState);
    }

    public void setTargetState(TargetArea target, Vector2 attackPosition)
    {
        this.attackTarget = target;
        this.unqiueTarget = attackPosition;
        this.setNewState(EnemyStateType.TargetRun);
    }


    public void OnVelocityComputed(Vector2 safeVelocity)
    {
        Velocity = safeVelocity;
    }
}
