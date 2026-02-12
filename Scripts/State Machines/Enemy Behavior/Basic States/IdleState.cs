using System;
using Godot;
using Godot.Collections;

namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public partial class IdleState : EnemyState
{
    private readonly string AnimationPrefix = "_Idle";
    private EnemyStateType? previousState;
    private string idleDir = "";
    public override void _enterState(EnemyBody body, EnemyStateType? prevState)
    {
        previousState = prevState;
        body.Velocity = Vector2.Zero;
        var targetDir = body.Position.DirectionTo(Vector2.Zero);
        body.AnimationPrefix = AnimationPrefix;

        body.sprite.Play();
    }
    public override void _exitState(EnemyBody body, EnemyStateType nextState)
    {

    }
    public override void _processTick(EnemyBody body)
    {

        if (body.attackTarget != null)
        {
            if (body.Position.DistanceTo(body.unqiueTarget) < 5)
            {
                body.setNewState(EnemyStateType.Attack);
                return;
            }
            else
            {
                body.setNewState(EnemyStateType.TargetRun);
                return;
            }
        }
        else
        {
            body.setNewState(EnemyStateType.Run);
            return;
        }
    }
    public override EnemyStateType getStateType()
    {
        return EnemyStateType.Idle;
    }

}