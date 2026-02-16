using System.Data;
using Godot;
using System.Collections.Generic;
using CastleDefender.Scripts.World;
using System.Runtime.CompilerServices;
using System;

namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public partial class EnemyStateMachine
{

    public readonly static Dictionary<string, EnemyStateMachine> Templates = new Dictionary<string, EnemyStateMachine>()
    {
      {"basic",new EnemyStateMachine([new AttackState(), new DeathState(), new HurtState(), new IdleState(), new RunState(), new TargetRun()])}
    };
    private readonly static EnemyState[] BasicState = [new AttackState(), new DeathState(), new HurtState(), new IdleState(), new RunState(), new TargetRun()];

    public EnemyStateType CurrentState { get; private set; }
    private EnemyStateType _newState;
    public EnemyStateType NewState { get => _newState; set { _newState = value; } }
    private Dictionary<EnemyStateType, EnemyState> StateMemory = new Dictionary<EnemyStateType, EnemyState>();

    private EnemyBody _parent;
    public EnemyBody Parent { get => _parent; set { _parent = value; ParentChanged(); } }
    private EnemyStateMachine(EnemyState[] states)
    {
        foreach (var state in states)
            StateMemory[state.getStateType()] = state;

    }

    public EnemyStateMachine(EnemyStateMachine template)
    {
        this.CurrentState = template.CurrentState;
        this.NewState = template.NewState;
        this.StateMemory = template.StateMemory;
    }

    public void ParentChanged()
    {
        NewState = CurrentState;
        StateMemory[CurrentState]._enterState(Parent, null);
        if (Parent.debug)
        {
            Parent.debugLabel.Text = CurrentState.ToString();
        }
    }
    public void ProcessPhysics()
    {
        StateMemory[CurrentState]._processTick(Parent);

        if (this.CurrentState != this.NewState)
            ChangeState();

    }

    public void setNewState(EnemyStateType newState)
    {
        if (this.NewState == EnemyStateType.Death)
            return;
        this.NewState = newState;
    }
    private void ChangeState()
    {
        if (this.CurrentState == EnemyStateType.Death)
            return;
        StateMemory[CurrentState]._exitState(Parent, NewState);
        StateMemory[NewState]._enterState(Parent, CurrentState);
        CurrentState = NewState;
        //GD.Print(Parent.Name + " | " + Parent.spawner.Name + ": " + CurrentState);
    }

    public void OnAnimationFinished()
    {
        //GD.Print(Parent.Name + " | " + Parent.spawner.Name + " animation done");
        switch (CurrentState)
        {
            case EnemyStateType.Hurt:
                if (Parent.stats.Health <= 0)
                    setNewState(EnemyStateType.Death);
                else
                    setNewState(EnemyStateType.Idle);
                break;
            case EnemyStateType.Death:
                Parent.Kill();
                break;
            default:
                break;
        }

    }

}


public static class EnemyLogic
{
    public static FieldHandler FieldHandler;


    private static Dictionary<EnemyStateType, string> AnimationPrefixes = new Dictionary<EnemyStateType, string>()
    {
        {EnemyStateType.Idle,"_Idle"},
        {EnemyStateType.Attack,"_Attack"},
        {EnemyStateType.TargetRun,"_Run"},
        {EnemyStateType.Run,"_Run"},
        {EnemyStateType.Hurt,"_Hurt"},
        {EnemyStateType.Death,"_Death"}
    };

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
    private static Vector2 GetTargetVelocity(EnemyData enemy)
    {
        if (enemy.Target.X < 0 || enemy.Target.Y < 0)
            return Vector2.Zero;
        return enemy.Position.DirectionTo(enemy.Target) * enemy.Speed;
    }

    private static Vector2 GetFieldVelocity(EnemyData enemy)
    {
        var pos = FieldHandler.LocalToMap(enemy.Position);
        var vel = FieldHandler.GetDirectionAt(pos);
        return vel * enemy.Speed;
    }

    public static void HandleBasicEnemy(ref EnemyData enemy)
    {
        enemy.sprite.Animation = GetAnimationPrefix(enemy.CurrentState) + GetAnimationDirection(enemy.Velocity);
        enemy.sprite.Play();
        var hasTarget = enemy.Target != Vector2.Zero;
        var atTarget = hasTarget ? enemy.Position.DistanceTo(enemy.Target) <= 5 : false;

        switch (enemy.CurrentState)
        {
            case EnemyStateType.Idle:
                if (!hasTarget)
                {
                    enemy.CurrentState = EnemyStateType.Run;
                    break;
                }
                if (hasTarget && atTarget)
                {
                    enemy.CurrentState = EnemyStateType.Attack;
                    break;
                }
                if (hasTarget && !atTarget)
                {
                    enemy.CurrentState = EnemyStateType.TargetRun;
                    break;
                }
                break;

            case EnemyStateType.Run:
                enemy.Velocity = GetFieldVelocity(enemy);
                break;

            case EnemyStateType.TargetRun:
                if (!hasTarget)
                {
                    enemy.CurrentState = EnemyStateType.Idle;
                    break;
                }
                if (hasTarget && atTarget)
                {
                    enemy.CurrentState = EnemyStateType.Attack;
                    break;
                }

                break;

            case EnemyStateType.Attack:
                if (!hasTarget)
                    enemy.CurrentState = EnemyStateType.Idle;
                break;

            case EnemyStateType.Hurt:
                if (enemy.animCounter-- == 0)
                    enemy.CurrentState = EnemyStateType.Idle;
                break;

            case EnemyStateType.Death:
                if (enemy.animCounter-- == 0)
                    enemy.sprite.QueueFree();
                break;
        }
    }
}