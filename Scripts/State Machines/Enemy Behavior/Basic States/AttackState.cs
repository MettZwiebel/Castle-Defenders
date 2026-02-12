using System;
using Godot;
using Godot.Collections;

namespace CastleDefender.Scripts.Enemy.BehaviorMachine;

public partial class AttackState : EnemyState
{
    private readonly string AnimationPrefix = "_Attack";
    private bool IsDead = false;
    public override void _enterState(EnemyBody body, EnemyStateType? prevState)
    {
        if (body.attackTarget == null)
        {
            body.setNewState(EnemyStateType.Idle);
            return;
        }
        body.agent.SetAvoidanceLayerValue(body.avoidanceLayer, false);
        body.agent.SetAvoidanceMaskValue(body.avoidanceLayer, false);

        var dir = body.Position.DirectionTo(body.attackTarget.Position);

        body.AnimationPrefix = AnimationPrefix;
        body.attackTarget.updateEnemy(body, true);

        body.sprite.Play();
    }

    public override void _exitState(EnemyBody body, EnemyStateType nextState)
    {
        body.agent.SetAvoidanceLayerValue(body.avoidanceLayer, true);
        body.agent.SetAvoidanceMaskValue(body.avoidanceLayer, true);
        body.attackTarget?.updateEnemy(body, false);
    }
    public override void _processTick(EnemyBody body)
    {
        if (body.attackTarget == null)
            body.setNewState(EnemyStateType.Idle);
    }
    public override EnemyStateType getStateType()
    {
        return EnemyStateType.Attack;
    }

}