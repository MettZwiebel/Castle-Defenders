

using System;
using System.Collections.Generic;
using Godot;


namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public partial class RunState : EnemyState
{
    private readonly string AnimationPrefix = "_Run";
    private Vector2I lastCellCoords = Vector2I.Zero;
    public override void _enterState(EnemyBody body, EnemyStateType? prevState)
    {
        body.AnimationPrefix = AnimationPrefix;

        body.sprite.Play();
    }
    public override void _exitState(EnemyBody body, EnemyStateType nextState)
    {

    }
    public override void _processTick(EnemyBody body)
    {
        var mapCoords = body.FieldHandler.LocalToMap(body.Position);

        var dir = body.FieldHandler.GetDirectionAt(mapCoords);


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
        return EnemyStateType.Run;
    }
}