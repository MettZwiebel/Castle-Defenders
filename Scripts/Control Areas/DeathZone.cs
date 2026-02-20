using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class DeathZone : Area2D
{
    private Rid _shapeRid;
    private PhysicsShapeQueryParameters2D _query;
    public EnemyDirector director;
    private HashSet<Rid> toDamage = new HashSet<Rid>();
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
        _query.Transform = GlobalTransform;
    }

    public void OnTimeout()
    {
        CheckCollisions();
        foreach (var rid in toDamage)
        {
            director.DamageEnemy(rid, 500);
        }
    }

    private void CheckCollisions()
    {
        var spaceState = GetWorld2D().DirectSpaceState;

        // Update the query transform to match the current node position/rotation


        var results = spaceState.IntersectShape(_query, 1024);
        toDamage.Clear();
        foreach (var result in results)
        {
            Rid victimRid = (Rid)result["rid"];
            if (!toDamage.Contains(victimRid))
                toDamage.Add(victimRid);
        }

    }

}
