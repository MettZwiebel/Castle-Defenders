using System;
using System.Collections.Generic;
using Godot;


namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public partial class TargetRun : EnemyState
{
    private readonly string AnimationPrefix = "_Walk";
    public override void _enterState(EnemyBody body, EnemyStateType? prevState)
    {
        body.AnimationPrefix = AnimationPrefix;
        body.agent.SetAvoidanceLayerValue(body.avoidanceLayer, false);
        body.agent.SetAvoidanceMaskValue(body.avoidanceLayer, false);

        body.sprite.Play();
    }
    public override void _exitState(EnemyBody body, EnemyStateType nextState)
    {
        if (nextState == EnemyStateType.Attack)
            return;
        body.agent.SetAvoidanceLayerValue(body.avoidanceLayer, true);
        body.agent.SetAvoidanceMaskValue(body.avoidanceLayer, true);
    }
    public override void _processTick(EnemyBody body)
    {
        if (body.attackTarget == null)
        {
            body.stateMachine.setNewState(EnemyStateType.Idle);
            return;
        }
        if (body.Position.DistanceTo(body.unqiueTarget) < 5)
        {
            body.stateMachine.setNewState(EnemyStateType.Attack);
            return;
        }
        var dir = body.Position.DirectionTo(body.unqiueTarget);
        if (body.agent.AvoidanceEnabled && body.Velocity != Vector2.Zero)
        {
            body.agent.Velocity = dir * body.stats.Speed;
        }
        else
        {
            body.OnVelocityComputed(dir * body.stats.Speed);
        }
    }

    public override EnemyStateType getStateType()
    {
        return EnemyStateType.TargetRun;
    }
}