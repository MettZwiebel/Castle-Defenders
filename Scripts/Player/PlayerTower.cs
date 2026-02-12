using CastleDefender.Scripts.Weapons;
using Godot;
using System;

public partial class PlayerTower : Node2D
{


    public Weapon weapon = new BallisticWeapon();

    public override void _Ready()
    {
        base.AddChild(weapon);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Click"))
        {
            weapon.fireWeapon(base.Position.DirectionTo(GetGlobalMousePosition()));
        }
    }
}
