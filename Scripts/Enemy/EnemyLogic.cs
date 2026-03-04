using System.Data;
using Godot;
using System.Collections.Generic;
using CastleDefender.Scripts.World;
using System.Runtime.CompilerServices;
using System;

namespace CastleDefender.Scripts.Enemy;

public static class EnemyLogic
{
    public static FieldHandler FieldHandler;


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
                enemy.Velocity = GetTargetVelocity(enemy);
                break;

            case EnemyStateType.Attack:
                if (!hasTarget)
                    enemy.CurrentState = EnemyStateType.Idle;
                break;

            case EnemyStateType.Hurt:
                enemy.Velocity = Vector2.Zero;
                enemy.SafeVelocity = Vector2.Zero;

                if (enemy.animCounter-- <= 0)
                {
                    enemy.CurrentState = EnemyStateType.Idle;
                }
                break;

            case EnemyStateType.Death:
                enemy.Velocity = Vector2.Zero;
                enemy.SafeVelocity = Vector2.Zero;

                if (enemy.animCounter-- <= 0)
                {
                    enemy.pendingDead = true;
                }
                break;
        }
    }
}