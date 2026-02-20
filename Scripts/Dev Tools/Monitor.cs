using Godot;
using System;

public partial class Monitor : Label
{
    private Timer timer = new Timer();


    public override void _Ready()
    {
        
        timer.Autostart = true;
        timer.WaitTime = 1;
        timer.Timeout += OnTimerOut;
        base.AddChild(timer);
    }

    private void OnTimerOut()
    {
        var t = Performance.GetMonitor(Performance.Monitor.TimeFps).ToString() + " FPS";
        Text = t;
    }

}
