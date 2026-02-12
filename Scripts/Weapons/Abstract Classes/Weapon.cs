using Godot;

namespace CastleDefender.Scripts.Weapons;

public abstract partial class Weapon : Node
{

    public abstract void fireWeapon(Vector2 Direction);
    public abstract void stopWeapon();
    public abstract void updateStats(WeaponStats stats);

}