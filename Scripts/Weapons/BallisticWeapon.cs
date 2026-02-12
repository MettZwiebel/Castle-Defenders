using Godot;

namespace CastleDefender.Scripts.Weapons;

public partial class BallisticWeapon : Weapon
{
    public PackedScene projectileScene = GD.Load<PackedScene>("res://Scenes/Player/Projectile.tscn");
    public WeaponStats Stats;

    public override void fireWeapon(Vector2 Direction)
    {
        var projectile = (Projectile)projectileScene.Instantiate();
        projectile.Velocity = Direction;
        var parent = base.GetParent();
        parent.AddChild(projectile);
    }

    public override void stopWeapon()
    {
    }

    public override void updateStats(WeaponStats stats)
    {
        this.Stats = stats;
    }

}
