using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class ExpandingMenu : Node2D
{
    private Array<Buildable> _items;

    [Export] public Array<Buildable> Items { get => _items; set { _items = value; Rebuild(); } }

    private Node2D DisplayNode = new Node2D();
    private PackedScene ExpandingButtonsScene = GD.Load<PackedScene>("res://Scenes/UI/Elements/ExpandingButtons.tscn");
    private ExpandingButtons UpperButtons, LowerButtons;
    private int _activeMenu = -1;
    private Array<Buildable> MenuCategories = new Array<Buildable>()
    {
        {new Buildable("build","res://assets/UI Stock/3 Icons/Icon with back/Icon_41.png","Construction","")},
        {new Buildable("tower","res://assets/UI Stock/3 Icons/Icon with back/Icon_28.png","Towers","")},
        {new Buildable("pop","res://assets/UI Stock/3 Icons/Icon with back/Icon_33.png","Population","")},
    };

    [Export] public int ActiveMenu { get => _activeMenu; set { _activeMenu = value; ChangeActiveMenu(); } }

    [Signal] public delegate void OnItemSelectedEventHandler(string itemName);
    [Signal] public delegate void OnCategorySelectedEventHandler(string Category);

    public ExpandingMenu() : base()
    {
        LowerButtons = (ExpandingButtons)ExpandingButtonsScene.Instantiate();
        UpperButtons = (ExpandingButtons)ExpandingButtonsScene.Instantiate();

        LowerButtons.Items = MenuCategories;

        LowerButtons.DisplayOffset = new Vector2I(0, 11);
        UpperButtons.DisplayOffset = new Vector2I(0, -11);

        LowerButtons.ZIndex = 100;
    }

    public override void _Ready()
    {
        LowerButtons.OnBarClicked += OnLowerBarClicked;
        UpperButtons.OnBarClicked += OnUpperBarClicked;
        LowerButtons.ZIndex = 100;
        base.AddChild(LowerButtons);
        base.AddChild(UpperButtons);
    }

    private void Rebuild()
    {
        ChangeActiveMenu();
    }

    private void ChangeActiveMenu()
    {
        if (ActiveMenu < 0)
        {
            UpperButtons.Items = new Array<Buildable>();
            return;
        }
        var filtered = Items.Where<Buildable>(item => item.Category == MenuCategories[ActiveMenu].UniqueName);
        UpperButtons.Items = new Array<Buildable>(filtered);
    }


    private void OnLowerBarClicked(int ButtonIndex, string itemName)
    {
        if (ButtonIndex == ActiveMenu)
            ActiveMenu = -1;
        else
            ActiveMenu = ButtonIndex;
        EmitSignal(SignalName.OnCategorySelected, itemName);
    }

    private void OnUpperBarClicked(int ButtonIndex, string itemName)
    {
        EmitSignal(SignalName.OnItemSelected, itemName);
    }
}
