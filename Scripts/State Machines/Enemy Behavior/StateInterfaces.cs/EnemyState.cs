
using Godot;

namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public abstract partial class EnemyState
{
    public abstract void _enterState(EnemyBody body, EnemyStateType? prevState);
    public abstract void _exitState(EnemyBody body, EnemyStateType nextState);
    public abstract void _processTick(EnemyBody body);
    public abstract EnemyStateType getStateType();
}