using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BattleHandler : Node
{
    //private EnemyHandler EnemyHandler;
    private EnemyDirector director = new EnemyDirector();

    public BattleHandler(FieldHandler fieldHandler) : base()
    {
        
    }

    [Signal]
    public delegate void OnBattleEndedEventHandler();

    public override void _Ready()
    {
        //EnemyHandler = new EnemyHandler(FieldHandler, 1024);


        //base.AddChild(EnemyHandler);

        //EnemyHandler.OnBudgetSpent += OnBudgetSpent;

        director.EnemyMaximum = 1000;
        director.Budget = 1000;
        base.AddChild(director);
    }

    public void StartBattle(uint day)
    {
        //EnemyHandler.StartBattle(5000);
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
