using System.Data;
using Godot;
using System.Collections.Generic;

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