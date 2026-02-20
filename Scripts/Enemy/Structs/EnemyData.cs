using CastleDefender.Scripts.Enemy;
using Godot;
using System;
using System.Collections.Generic;

public struct EnemyData
{
    private static readonly float AnimationTicks = 10;
    public int Health = 100;

    public Rid PhysicsBody, PhysicsShape, Agent;
    private Vector2 _position;
    public Vector2 Position { get => _position; set { _position = value; } }
    public Vector2 Velocity, SafeVelocity, LastPosition, Target;
    public float Speed = 100;
    public uint layer;
    public EnemyStateType CurrentState;
    public bool isAlive, pendingDead;
    public AnimatedSprite2D sprite;
    public float animCounter;

    public EnemyData()
    {
        this.sprite = new AnimatedSprite2D();
        this.Position = new Vector2(-1, -1);
        this.LastPosition = this.Position;
        this.Velocity = new Vector2(0, 0);
        this.SafeVelocity = new Vector2(0, 0);
        this.Speed = 0;
        this.CurrentState = EnemyStateType.Death;
    }
    public EnemyData(Vector2 Position, Vector2 Velocity, float Speed, SpriteFrames frames)
    {
        this.sprite = new AnimatedSprite2D();
        this.Position = Position;
        this.LastPosition = this.Position;
        this.Velocity = Velocity;
        this.SafeVelocity = new Vector2(0, 0);
        this.Speed = Speed;
        this.CurrentState = EnemyStateType.Idle;

        this.sprite.SpriteFrames = frames;
    }

    public void ReviveEnemy(Vector2 Position, Vector2 Velocity, float Speed, SpriteFrames frames)
    {
        this.Health = 100;

        this.Position = Position;
        this.LastPosition = this.Position;
        this.Velocity = Velocity;
        this.SafeVelocity = new Vector2(0, 0);
        this.Speed = Speed;
        this.CurrentState = EnemyStateType.Idle;

        this.isAlive = true;
        this.pendingDead = false;
        this.sprite.Position = this.Position;
        this.sprite.SpriteFrames = frames;
        this.sprite.Visible = true;
        sprite.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    public void Kill()
    {
        isAlive = false;
        pendingDead = false;
        sprite.Visible = false;
        sprite.ProcessMode = Node.ProcessModeEnum.Disabled;
    }

    public void takeDamage(int damage)
    {
        if (CurrentState == EnemyStateType.Death)
            return;
        Health -= damage;
        if (Health <= 0)
            CurrentState = EnemyStateType.Death;
        else
            CurrentState = EnemyStateType.Hurt;
        animCounter = AnimationTicks;
    }

}