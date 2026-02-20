using Godot.Collections;
using Godot;
using System.Linq;


public partial class Projectile : Area2D
{
    [Export]
    public float speed;
    [Export]
    public int health = 50;
    public Vector2 Velocity = Vector2.Zero;
    public float MaxTravelDistance = 2048;
    private float distance = 0;

    private Rid _shapeRid;
    private PhysicsShapeQueryParameters2D _query;

    [Signal] public delegate void OnEnemyHitEventHandler(Array<Rid> enemies);

    public override void _Ready()
    {
        // 1. Get the RID of the first shape attached to this Area2D
        var shapeOwner = GetShapeOwners()[0];
        _shapeRid = ShapeOwnerGetShape((uint)shapeOwner, 0).GetRid();

        // 2. Pre-configure the query to save CPU cycles
        _query = new PhysicsShapeQueryParameters2D
        {
            ShapeRid = _shapeRid,
            CollisionMask = 2, // Ensure this matches your Enemy Collision Layer
            CollideWithAreas = false,
            CollideWithBodies = true
        };

    }

    private void CheckCollisions()
    {
        var spaceState = GetWorld2D().DirectSpaceState;

        // Update the query transform to match the current node position/rotation
        _query.Transform = GlobalTransform;

        var results = spaceState.IntersectShape(_query, 16).Select(result => (Rid)result["rid"]).ToArray();
        if (results.Length > 0)
        {
            EmitSignal(SignalName.OnEnemyHit, results);
            this.QueueFree();
        }

    }


    public override void _PhysicsProcess(double delta)
    {
        distance += (Velocity * speed).Length();
        if (distance >= MaxTravelDistance)
            QueueFree();
        base.Position += Velocity * speed;
        CheckCollisions();
    }

}