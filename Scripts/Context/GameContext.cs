using System.Threading.Tasks;
using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;

public partial class GameContext : Node2D
{
    private const int Width = 100;
    private const int Height = 100;


    private MapHandler MapHandler;
    private FieldHandler FieldHandler;
    private VectorFieldService VectorCalc;
    private ConstructionManager ConstructionManager;
    private ProfileStats ProfileStats;
    private BattleHandler BattleHandler;
    private BuildHandler BuildHandler;
    [Export]
    public Node2D DisplayNode;

    [Export]
    private bool Debug = false;
    private Node2D debugNode = new Node2D();

    private bool BattleInProgress = false;

    public GameContext(Vector2I Dimensions, MapHandler mapHandler, BattleHandler battleHandler) : base()
    {
        this.BattleHandler = battleHandler;
        this.MapHandler = mapHandler;
        ProfileStats = new ProfileStats();
    }

    public GameContext() : base() { }
    public override void _Ready()
    {

        ProfileStats = new ProfileStats();
        MapHandler = new MapHandler();
        FieldHandler = new FieldHandler(200,200);
        BattleHandler = new BattleHandler(FieldHandler);

        ConstructionManager = new ConstructionManager(MapHandler, ProfileStats, DisplayNode);
        
        base.AddChild(MapHandler);
        base.AddChild(ConstructionManager);

        Load();

        VectorCalc = new VectorFieldService(200,200, MapHandler.GetScaledGates(new Vector2I(2,2)), MapHandler.GetScaledWalls(new Vector2I(2,2)), Debug);

        FieldHandler.UpdateVectorField(VectorCalc.Calculate(new Vector2I(99,99)));

        if (Debug)
        {
            AddChild(debugNode);
            debugNode.AddChild(VectorCalc.GetDebugTree());
        }


        BattleHandler.Connect(BattleHandler.SignalName.OnBattleEnded, Callable.From(OnBattleEnded));

        BuildHandler = new BuildHandler();



        base.AddChild(BattleHandler);
        //Save();
    }

    public override async void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("Space"))
        {
            StartBattle();
        }
        if (@event.IsActionPressed("Click"))
        {
            var t = FieldHandler.LocalToMap(GetGlobalMousePosition());
            var vecs = await RecalculateVectors(t);
            FieldHandler.UpdateVectorField(vecs);
            if (Debug)
                debugNode.AddChild(VectorCalc.GetDebugTree());
        }
    }

    private async Task<Godot.Vector2[,]> RecalculateVectors(Vector2I target)
    {
        return VectorCalc.Calculate(target);
    }


    private void StartBattle()
    {
        if (BattleInProgress)
            return;
        BattleHandler.StartBattle(ProfileStats.CurrentDay);
        BattleInProgress = true;
        GD.Print("Started Battle for day" + ProfileStats.CurrentDay);
    }

    private void OnBattleEnded()
    {
        GD.Print("Game Context: Battle Ended");
        BattleInProgress = false;
    }


    public void Save()
    {
        var dict = new Dictionary<string, Variant>()
        {
            {"Stats",ProfileStats.Save()},
            {"MapHandler", MapHandler.Save()},
            {"BattleHandler",BattleHandler.Save()}
        };
        SavingSystem.Save(SavingSystem.DefaultSaveLocation + ProfileStats.SavefileName, dict);
    }

    public void Load()
    {
        var dict = SavingSystem.Load(SavingSystem.DefaultSaveLocation + ProfileStats.SavefileName);

        foreach (var key in dict.Keys)
        {
            switch (key)
            {
                case "Stats":
                    ProfileStats.Load(dict[key].As<Dictionary<string, Variant>>());
                    break;
                case "MapHandler":
                    MapHandler.Load(dict[key].As<Dictionary<string, Variant>>());
                    break;
                case "BattleHandler":
                    BattleHandler.Load(dict[key].As<Dictionary<string, Variant>>());
                    break;
            }
        }
    }

}
