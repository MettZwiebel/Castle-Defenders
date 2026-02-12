using Godot;


namespace CastleDefender.Scripts.Weapons;

public partial class RaycastWeapon : Weapon
{
    private RayCast2D raycast = new RayCast2D();
    private Node2D parent;
    public override void fireWeapon(Vector2 Direction)
    {
        if (raycast.IsColliding())
        {
            var body = raycast.GetCollider();
            if (body is CharacterBody2D)
            {
                ((CharacterBody2D)body).QueueFree();
            }
        }
    }

    public override void stopWeapon()
    {

    }

    public override void updateStats(WeaponStats stats)
    {

    }


    public override void _Ready()
    {
        raycast.SetCollisionMaskValue(1, false);
        raycast.SetCollisionMaskValue(2, true);
        raycast.SetCollisionMaskValue(3, true);
        base.AddChild(raycast);

        parent = (Node2D)GetParent();
    }

    public override void _PhysicsProcess(double delta)
    {
        raycast.TargetPosition = parent.GetGlobalMousePosition();
    }



}
