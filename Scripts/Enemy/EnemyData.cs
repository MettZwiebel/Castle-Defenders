using CastleDefender.Scripts.Enemy.BehaviorMachine;
using Godot;


public struct EnemyData
{
    private static readonly float AnimationTicks = 20;
    public int Health = 100;

    public Rid PhysicsBody;
    private Vector2 _position;
    public Vector2 Position { get => _position; set { _position = value; } }
    public Vector2 LastPosition;
    public Vector2 Velocity;
    public Vector2 Target = Vector2.Zero;
    public float Speed = 100;

    public EnemyStateType CurrentState;

    public AnimatedSprite2D sprite;
    public float animCounter;

    public EnemyData()
    {
        this.sprite = new AnimatedSprite2D();
        this.Position = new Vector2(-1, -1);
        this.Velocity = new Vector2(0, 0);
        this.Speed = 0;

        this.CurrentState = EnemyStateType.Death;
    }
    public EnemyData(Vector2 Position, Vector2 Velocity, float Speed, SpriteFrames frames)
    {
        this.sprite = new AnimatedSprite2D();
        this.Position = Position;
        this.Velocity = Velocity;
        this.Speed = Speed;
        this.CurrentState = EnemyStateType.Idle;

        this.sprite.SpriteFrames = frames;
        this.sprite.Play();
    }

    public void ReviveEnemy(Vector2 Position, Vector2 Velocity, float Speed, SpriteFrames frames)
    {
        this.sprite = new AnimatedSprite2D();
        this.Position = Position;
        this.Velocity = Velocity;
        this.Speed = Speed;
        this.CurrentState = EnemyStateType.Idle;

        this.sprite.SpriteFrames = frames;
    }

    public bool isDead()
    {
        return CurrentState == EnemyStateType.Death && animCounter <= 0;
    }

    public void takeDamage(int damage)
    {
        Health -= damage;
        if (Health < 0)
            CurrentState = EnemyStateType.Idle;
        else
            CurrentState = EnemyStateType.Death;
        animCounter = AnimationTicks;
    }
}