using CastleDefender.Scripts.Enemy.BehaviorMachine;
using Godot;


public struct EnemyData
{
    private static readonly float AnimationTicks = 10;
    public int Health = 100;

    public Rid PhysicsBody, Agent;
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
        this.sprite = new AnimatedSprite2D();
        this.Position = Position;
        this.LastPosition = this.Position;
        this.Velocity = Velocity;
        this.SafeVelocity = new Vector2(0, 0);
        this.Speed = Speed;
        this.CurrentState = EnemyStateType.Idle;

        this.isAlive = true;
        this.pendingDead = false;
        this.sprite.SpriteFrames = frames;
    }

    public void Kill()
    {
        isAlive = false;
        pendingDead = false;
        PhysicsServer2D.BodySetSpace(PhysicsBody, new Rid());

        NavigationServer2D.FreeRid(Agent);
        PhysicsServer2D.FreeRid(PhysicsBody);
        sprite.QueueFree();
    }

    public void takeDamage(int damage)
    {
        if (CurrentState == EnemyStateType.Hurt || CurrentState == EnemyStateType.Death)
            return;
        Health -= damage;
        if (Health <= 0)
            CurrentState = EnemyStateType.Death;
        else
            CurrentState = EnemyStateType.Hurt;
        animCounter = AnimationTicks;
    }
}