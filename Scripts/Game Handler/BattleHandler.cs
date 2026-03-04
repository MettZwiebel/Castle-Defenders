using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BattleHandler : Node
{
    public EnemyDirector director;
    private FieldHandler fieldHandler;
    private MapHandler mapHandler;
    private SpatialEntityGrid entityGrid;

    private uint _maximum;
    public uint EnemyMaximum { get => _maximum; set { _maximum = value; CreateEntityGrid(); } }
    public BattleHandler(FieldHandler fieldHandler, MapHandler mapHandler) : base()
    {
        this.fieldHandler = fieldHandler;
        this.mapHandler = mapHandler;
        this.entityGrid = new SpatialEntityGrid(fieldHandler.FieldDimensions, 50);
        this.director = new EnemyDirector(mapHandler, fieldHandler, entityGrid);
    }

    [Signal]
    public delegate void OnBattleEndedEventHandler();

    public override void _Ready()
    {
        base.AddChild(director);

        //var tower = new Tower(new Vector2(1136, 1136), 512, 128, fieldHandler, director, entityGrid);
        //base.AddChild(tower);
    }

    private void CreateEntityGrid()
    {
        entityGrid.Resize(fieldHandler.FieldDimensions, EnemyMaximum);
    }

    public void StartBattle(uint day)
    {
        this.EnemyMaximum = 5000;
        director.EnemyMaximum = 5000;
        director.Budget = 1000000;

        director.StartProcessing();
    }

    private void OnBudgetSpent()
    {
        GD.Print("Battle Handler: Budget Spent");
        EmitSignal(SignalName.OnBattleEnded);
    }

    public Dictionary<string, Variant> Save()
    {
        return new Dictionary<string, Variant>()
        {
            //Need to save towers
        };
    }

    public void Load(Dictionary<string, Variant> dict)
    {
        foreach (var key in dict.Keys)
        {
            switch (key)
            {
                default: break;
            }
        }
    }
}
