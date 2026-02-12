using Godot;
using Godot.Collections;

public partial class TowerArea : TargetArea
{
    public Label label = new Label();
    private readonly Dictionary<int, int[]> ConnectedQuadrants = new Dictionary<int, int[]>();
    private readonly Dictionary<int, Vector2> WalloffsetMap = new Dictionary<int, Vector2>();
    private readonly Dictionary<EnemyBody, int> DamageMap = new Dictionary<EnemyBody, int>();
    private int DamageCount = 0;
    private int health = 1000;

    private PackedScene projectile = GD.Load<PackedScene>("res://Scenes/Player/Projectile.tscn");
    public void PopulateConnectedQuadrants(int[] q0, int[] q1, int[] q2, int[] q3)
    {
        ConnectedQuadrants.Clear();

        ConnectedQuadrants[0] = q0;
        ConnectedQuadrants[1] = q1;
        ConnectedQuadrants[2] = q2;
        ConnectedQuadrants[3] = q3;
    }
    public void PopulateWallOffset(Vector2 q0, Vector2 q1, Vector2 q2, Vector2 q3)
    {
        WalloffsetMap.Clear();

        WalloffsetMap[0] = q0;
        WalloffsetMap[1] = q1;
        WalloffsetMap[2] = q2;
        WalloffsetMap[3] = q3;
    }

    public override void _Ready()
    {
        QuadrantDistributer.InitializeTemplate(160f);

        PopulateConnectedQuadrants(new int[] { 0, 3 }, new int[] { 1 }, new int[] { 2, 3 }, new int[] { 3, 0, 2 });
        PopulateWallOffset(new Vector2(-16, 16), new Vector2(16, -16), new Vector2(-16, 16), new Vector2(-16, 16));
    }
    public override void registerEnemy(EnemyBody body)
    {
        if (body.attackTarget != null)
            return;
        DamageMap[body] = body.stats.DamageToGates;
        body.setTargetState(this, GetAttackPosition(body));
    }

    public override void freeEnemy(EnemyBody body)
    {
        if (DamageMap.ContainsKey(body))
        {
            body.attackTarget = null;
            DamageCount -= DamageMap[body];
            DamageMap.Remove(body);
        }
    }
    public override void updateEnemy(EnemyBody body, bool isAttacking)
    {
        DamageCount += isAttacking ? DamageMap[body] : DamageMap[body] * -1;
    }
    public void OnBodyEntered(Node2D body)
    {
        if (body is EnemyBody && ((EnemyBody)body).stats.EnemyType != EnemyType.MELEE)
        {
            var enemy = (EnemyBody)body;
            registerEnemy(enemy);
        }
    }
    private void DisableArea()
    {
        foreach (var enemy in DamageMap.Keys)
        {
            freeEnemy(enemy);
        }
        DamageCount = 0;
        base.SetCollisionMaskValue(2, false);
        base.SetCollisionMaskValue(3, false);
    }

    public void OnDamageTimerTimeout()
    {
        health -= DamageCount;
        label.Text = health.ToString() + "\n" + DamageCount;
        if (health <= 0)
        {
            DisableArea();
        }
    }

    private Vector2 GetAttackPosition(EnemyBody body)
    {
        var dir = Position.DirectionTo(body.Position);
        var quadrant = QuadrantMapping(dir);
        return QuadrantDistributer.GetPosition(body.GetHashCode(), WalloffsetMap[quadrant], ConnectedQuadrants[quadrant]) + Position;
    }

    public int QuadrantMapping(Vector2 direction)
    {
        bool IsXPositive = direction.X >= 0;
        bool IsYPositive = direction.Y >= 0;

        if (IsXPositive && IsYPositive)
            return 0;
        if (IsXPositive && !IsYPositive)
            return 1;
        if (!IsXPositive && !IsYPositive)
            return 2;
        return 3;
    }
}
