using Godot;
using Godot.Collections;

namespace CastleDefender.Scripts.World;

public partial class FieldCell : Node
{
    public readonly Vector2I Position;
    public Vector2 Direction { get; set; }

    public int StaticWeight { get; set; }
    public int CalculatedWeight { get; set; }

    public bool CalculationDone { get; set; }
    public int EnemyCount = 0;


    public FieldCell(Vector2I Position, int StaticWeight) : base()
    {
        this.Position = Position;
        this.Direction = Vector2.Zero;
        this.StaticWeight = StaticWeight;
        this.CalculatedWeight = int.MaxValue;
        this.CalculationDone = false;
    }

    public override bool Equals(object obj)
    {
        if (obj is FieldCell)
            return this.Equals((FieldCell)obj);
        return base.Equals(obj);
    }

    public bool Equals(FieldCell cell)
    {
        return this.Position.Equals(cell.Position);
    }

    public override int GetHashCode()
    {
        return this.Position.GetHashCode();
    }
}