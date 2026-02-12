using Godot;
using Godot.Collections;
using System;
namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public partial class HurtState : EnemyState
{
    private readonly string AnimationPrefix = "_Hurt";
    private EnemyStateType? previousState;

    public override void _enterState(EnemyBody body, EnemyStateType? prevState)
    {
        body.agent.SetAvoidanceMaskValue(body.avoidanceLayer, false);
        body.agent.SetAvoidanceLayerValue(body.avoidanceLayer, false);
        body.AnimationPrefix = AnimationPrefix;
        body.Velocity = Vector2.Zero;
        previousState = prevState;

        body.sprite.Play();
    }

    public override void _exitState(EnemyBody body, EnemyStateType nextState)
    {
        if (nextState == EnemyStateType.Death)
            return;
        body.agent.SetAvoidanceMaskValue(body.avoidanceLayer, true);
        body.agent.SetAvoidanceLayerValue(body.avoidanceLayer, true);
    }
    public override void _processTick(EnemyBody body)
    {

    }
    public override EnemyStateType getStateType()
    {
        return EnemyStateType.Hurt;
    }


}