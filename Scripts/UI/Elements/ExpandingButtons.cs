using Godot;
using Godot.Collections;

[Tool]
public partial class ExpandingButtons : Node2D
{
    private Array<Buildable> _items;
    private Vector2I _dimensions;
    private Vector2I _offset;
    [Export] public Array<Buildable> Items { get => _items; set { _items = value; BuildBar(); } }
    [Export] public Vector2I ButtonDimensions { get => _dimensions; set { _dimensions = value; BuildBar(); } }
    [Export] public Vector2I DisplayOffset { get => _offset; set { _offset = value; BuildBar(); } }
    private Dictionary<Vector2, int> ButtonMapping = new Dictionary<Vector2, int>();
    private PackedScene ButtonScene = GD.Load<PackedScene>("res://Scenes/UI/Elements/HuDButton.tscn");
    private Node2D DisplayNode = new Node2D();

    [Signal] public delegate void OnBarClickedEventHandler(int buttonIndex, string itemName);


    public override void _Ready()
    {
        BuildBar();
    }

    private void BuildBar()
    {
        if (Engine.IsEditorHint())
        {
            var children = DisplayNode.GetChildren();
            for (int i = 0; i < children.Count; i++)
            {
                DisplayNode.RemoveChild(children[i]);
                children[i].QueueFree();
            }
        }

        DisplayNode.QueueFree();
        DisplayNode = new Node2D();
        base.AddChild(DisplayNode);
        ButtonMapping.Clear();



        var Offset = new Vector2I(ButtonDimensions.X, 0);
        var pos = new Vector2I(-((int)(Items.Count / 2) * Offset.X), 0);
        if (Items.Count % 2 == 0)
        {
            pos += new Vector2I(ButtonDimensions.X / 2, 0);
        }

        foreach (var item in Items)
        {
            var button = (HuDButton)ButtonScene.Instantiate();
            button.Item = item;
            button.Position = pos + DisplayOffset;
            pos += Offset;
            ButtonMapping[button.Position] = DisplayNode.GetChildCount();

            button.OnButtonClicked += OnButtonClicked;
            DisplayNode.AddChild(button);
        }
    }

    public void OnButtonClicked(Vector2I buttonPos)
    {
        var index = ButtonMapping[buttonPos];
        EmitSignal(SignalName.OnBarClicked, index, Items[index].UniqueName);
    }

}
