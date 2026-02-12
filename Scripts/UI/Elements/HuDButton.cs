using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class HuDButton : Node2D
{
    [Export] public Button button;
    [Export] public Buildable Item { get; set; }
    [Signal] public delegate void OnButtonClickedEventHandler(Vector2I buttonPos);


    public override void _Ready()
    {
        if (IsInstanceValid(Item))
            button.Icon = GD.Load<CompressedTexture2D>(Item.MenuIconPath);
        button.Pressed += OnButtonPressed;
    }

    private void Rebuild()
    {

    }

    public void OnButtonPressed()
    {
        EmitSignal(SignalName.OnButtonClicked, base.Position);
    }

}
