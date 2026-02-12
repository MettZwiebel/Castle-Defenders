using Godot;
using System;

public partial class PlayerTargeting : RayCast2D
{
    [Export]
    public bool isTargeting { get; set; }
    [Export]
    public Line2D line;
    [Export]
    public Timer timer;
    [Export]
    public bool debug;

    public override void _Ready()
    {
        line.Visible = debug;
    }


    public void OnTimerTimeout()
    {
        if (isTargeting)
        {
            base.TargetPosition = GetGlobalMousePosition();

            if (debug)
            {
                line.QueueFree();
                line = new Line2D();
                line.AddPoint(new Vector2(0, 0));
                line.AddPoint(TargetPosition);
                base.AddChild(line);
            }
        }
    }

}
