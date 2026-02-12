using System.Linq;
using Godot;
using Godot.Collections;

public partial class ConstructionManager : Node2D
{
    [Export] public Node2D DisplayNode;
    private TileMapLayer planningLayer = new TileMapLayer();
    private MapHandler MapHandler;
    private Array<Vector2I> buildQueue = new Array<Vector2I>();
    private ProfileStats stats;
    private ExpandingMenu buildMenu;
    private Array<Buildable> buildables = GD.Load<Buildables>("res://assets/Resources/Buildables.tres").List;
    private Sprite2D previewIcon;
    private bool isDrawing, isDragDraw, isDragRemove;
    private Vector2I lastCellCoords = new Vector2I(0, 0);
    private string selectedItem = "";
    public ConstructionManager() : base() { }

    public ConstructionManager(MapHandler mapHandler, ProfileStats stats, Node2D DisplayNode) : base()
    {
        this.MapHandler = mapHandler;
        this.stats = stats;
        this.DisplayNode = DisplayNode;
        previewIcon = new Sprite2D();

        buildMenu = new ExpandingMenu();
        buildMenu.Items = buildables;
        buildMenu.Position = new Vector2I(0, 400);
        buildMenu.Scale = new Vector2(4, 4);

        buildMenu.OnItemSelected += OnItemSelected;
        buildMenu.OnCategorySelected += OnCategorySelected;

        planningLayer.TileSet = GD.Load<TileSet>("res://assets/TileSets/Master.tres");
        planningLayer.ZIndex = 20;
        DisplayNode.AddChild(buildMenu);
        base.AddChild(previewIcon);
        base.AddChild(planningLayer);
    }

    public void exit()
    {
        DisplayNode.RemoveChild(buildMenu);
    }

    public void PlanTileAt(Vector2I coords, string buildingKey)
    {

        if (MapHandler.IsCellColliding(coords))
            return;
        GD.Print("Building");
        var item = buildables.Where(item => item.UniqueName == buildingKey).First();
        planningLayer.SetCell(coords, item.tileSid, item.tileAtlas, item.tileAlt);
    }

    public void RemoveTileAt(Vector2I coords)
    {
        if (MapHandler.IsCellColliding(coords))
        {

        }
        else
            planningLayer.EraseCell(coords);
    }

    public void OnItemSelected(string itemName)
    {
        if (selectedItem == itemName)
        {
            selectedItem = "";
            isDrawing = false;
            previewIcon.Visible = false;
        }
        else
        {
            selectedItem = itemName;
            var item = buildables.Where<Buildable>(item => item.UniqueName == selectedItem).First<Buildable>();
            previewIcon.Texture = GD.Load<CompressedTexture2D>(item.MenuIconPath);
            isDrawing = true;
            previewIcon.Visible = true;
        }
    }

    public void OnCategorySelected(string Category)
    {
        if (selectedItem != "")
        {
            selectedItem = "";
            isDrawing = false;
            previewIcon.Visible = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (isDrawing)
        {
            if (@event is InputEventMouseButton)
            {
                var e = (InputEventMouseButton)@event;
                if (@event.IsActionPressed("Click") && e.Pressed)
                    isDragDraw = true;
                if (@event.IsActionPressed("RMB") && e.Pressed)
                    isDragRemove = true;
                if (!e.Pressed)
                {
                    isDragDraw = false;
                    isDragRemove = false;
                }
            }
            if (@event.IsActionPressed("Click"))
            {
                lastCellCoords = MapHandler.LocalToMap(GetGlobalMousePosition());
                PlanTileAt(lastCellCoords, selectedItem);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (@event.IsActionPressed("RMB"))
            {
                lastCellCoords = MapHandler.LocalToMap(GetGlobalMousePosition());
                RemoveTileAt(lastCellCoords);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (isDrawing)
        {

            var pos = MapHandler.LocalToMap(GetGlobalMousePosition());
            var iconPos = MapHandler.MapToLocal(pos);
            if (isDragDraw && lastCellCoords != pos)
            {
                PlanTileAt(pos, selectedItem);
                lastCellCoords = pos;
            }
            if (isDragRemove && lastCellCoords != pos)
            {
                RemoveTileAt(pos);
                lastCellCoords = pos;
            }

            if (previewIcon.Position != iconPos)
                previewIcon.Position = iconPos;
        }
    }

    private Array<Vector2I> GetLinePoints(Vector2I p1, Vector2I p2)
    {
        Array<Vector2I> points = new Array<Vector2I>();
        float distance = p1.DistanceTo(p2);

        // We iterate based on distance to ensure no gaps
        for (int i = 0; i <= distance; i++)
        {
            float t = distance > 0 ? i / distance : 0;
            Vector2 interpolated = ((Vector2)p1).Lerp((Vector2)p2, t);
            points.Add(new Vector2I((int)Mathf.Round(interpolated.X), (int)Mathf.Round(interpolated.Y)));
        }
        return points;
    }


    public Dictionary<string, Variant> Save()
    {
        return new Dictionary<string, Variant>()
        {
            {"map",MapHandler.SaveTilemapLayer(planningLayer)},
            {"que",SaveArray(buildQueue)},
        };
    }

    public void Load(Dictionary<string, Variant> dict)
    {
        foreach (var key in dict.Keys)
        {
            switch (key)
            {
                case "map":
                    planningLayer = MapHandler.LoadTilemapLayer(dict[key].As<Dictionary<string, Variant>>());
                    break;
                case "que":
                    buildQueue = LoadArray(dict[key].As<Array<Dictionary<string, Variant>>>());
                    break;
            }
        }
    }

    private Array<Dictionary<string, Variant>> SaveArray(Array<Vector2I> arr)
    {
        Array<Dictionary<string, Variant>> result = new Array<Dictionary<string, Variant>>();
        foreach (var vec in arr)
        {
            result.Add(new Dictionary<string, Variant>()
            {
               {"X",vec.X},
               {"Y",vec.Y}
            });
        }
        return result;
    }

    private Array<Vector2I> LoadArray(Array<Dictionary<string, Variant>> arr)
    {
        Array<Vector2I> result = new Array<Vector2I>();
        foreach (var dict in arr)
        {
            var vec = Vector2I.Zero;
            foreach (var key in dict.Keys)
            {
                switch (key)
                {
                    case "X":
                        vec.X = dict[key].As<int>();
                        break;
                    case "Y":
                        vec.Y = dict[key].As<int>();
                        break;
                }
            }
            result.Add(vec);
        }
        return result;
    }

}
