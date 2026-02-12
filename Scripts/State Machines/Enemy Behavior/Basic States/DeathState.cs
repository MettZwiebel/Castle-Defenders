using Godot;
using Godot.Collections;
using System;

namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public partial class DeathState : EnemyState
{
    private readonly string AnimationPrefix = "_Death";
    private bool IsDead = false;

    public override void _enterState(EnemyBody body, EnemyStateType? prevState)
    {
        body.agent.SetAvoidanceMaskValue(body.avoidanceLayer, false);
        body.agent.SetAvoidanceLayerValue(body.avoidanceLayer, false);
        body.AnimationPrefix = AnimationPrefix;
        body.Velocity = Vector2.Zero;

        body.sprite.Play();
    }

    public override void _exitState(EnemyBody body, EnemyStateType nextState)
    {

    }
    public override void _processTick(EnemyBody body)
    {

    }
    public override EnemyStateType getStateType()
    {
        return EnemyStateType.Death;
    }
}