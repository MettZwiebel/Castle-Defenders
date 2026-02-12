using Godot;
using System;

public partial class GameCamera : Camera2D
{
    public Vector2 startDrag;
    public int speed = 10;
    public bool isDragging = false;
    [Export]
    public Node2D UINode;
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            var e = (InputEventMouseButton)@event;
            if (@event.IsActionPressed("MMB") && e.Pressed)
            {
                startDrag = GetGlobalMousePosition();
                isDragging = true;
            }
            if (!e.Pressed)
            {
                isDragging = false;
            }
        }
        if (@event.IsActionPressed("ZoomIn"))
        {
            base.Zoom /= 2;
            UINode.Scale *= 2;
        }
        if (@event.IsActionPressed("ZoomOut"))
        {
            base.Zoom *= 2;
            UINode.Scale /= 2;
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        if (isDragging)
        {
            var pos = GetGlobalMousePosition();
            var distance = pos.DistanceTo(startDrag);
            if (distance > 16)
                UINode.Position += pos.DirectionTo(startDrag) * (distance / 10);
        }
    }

}
