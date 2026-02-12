using Godot;


public partial class Projectile : Area2D
{
    [Export]
    public float speed = 100;
    [Export]
    public int health = 50;
    public Vector2 Velocity = Vector2.Zero;
    public float MaxTravelDistance = 1024;
    private float distance = 0;
    public void OnBodyEntered(Node body)
    {
        if (body is EnemyBody)
        {
            var enemy = (EnemyBody)body;
            enemy.takeDamage(health);
            this.QueueFree();
        }

    }


    public override void _PhysicsProcess(double delta)
    {
        distance += (Velocity * speed).Length();
        if (distance >= MaxTravelDistance)
            QueueFree();
        base.Position += Velocity * speed;
    }

}