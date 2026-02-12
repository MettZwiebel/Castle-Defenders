using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BattleHandler : Node
{
    private TowerHandler TowerHandler;
    private EnemyHandler EnemyHandler;
    private readonly FieldHandler FieldHandler;

    public BattleHandler(FieldHandler fieldHandler) : base()
    {
        this.FieldHandler = fieldHandler;
    }

    [Signal]
    public delegate void OnBattleEndedEventHandler();

    public override void _Ready()
    {
        TowerHandler = new TowerHandler();
        EnemyHandler = new EnemyHandler(FieldHandler, 1024);

        base.AddChild(TowerHandler);
        base.AddChild(EnemyHandler);

        EnemyHandler.OnBudgetSpent += OnBudgetSpent;
    }

    public void StartBattle(uint day)
    {
        EnemyHandler.StartBattle(5000);
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
