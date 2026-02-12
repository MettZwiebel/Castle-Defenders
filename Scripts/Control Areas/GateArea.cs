using Godot;
using System;
using Godot.Collections;

public partial class GateArea : TargetArea
{

    private Label label = new Label();
    private int health = 1000;
    private readonly Dictionary<EnemyBody, int> DamageMap = new Dictionary<EnemyBody, int>();
    private int damageCount = 0;
    [Export]
    public bool isVerticalGate = false;
    private const double PHI_CONJUGATE = 0.618033988749895;
    private int gateLength = 160;

    public override void _Ready()
    {
        label.Text = health.ToString();
    }

    public void OnBodyEntered(Node2D body)
    {
        if (body is EnemyBody)
        {
            registerEnemy((EnemyBody)body);
        }
    }

    public void OnBodyExited(Node2D body)
    {
        if (body is EnemyBody)
        {
            if (DamageMap.ContainsKey((EnemyBody)body))
                freeEnemy((EnemyBody)body);
        }
    }

    public override void registerEnemy(EnemyBody body)
    {
        if (body.attackTarget != null)
            return;
        body.setTargetState(this, calculateAttackPosition(body));
        DamageMap[body] = body.stats.DamageToGates;
    }
    public override void freeEnemy(EnemyBody body)
    {
        if (body.attackTarget != this)
            return;
        body.attackTarget = null;
        DamageMap.Remove(body);
    }

    public override void updateEnemy(EnemyBody body, bool isAttacking)
    {
        damageCount += isAttacking ? DamageMap[body] : DamageMap[body] * -1;
    }

    private Vector2 calculateAttackPosition(EnemyBody body)
    {
        var startPosition = body.Position;
        var hash = body.GetHashCode();
        double spread = Math.Abs(hash) * PHI_CONJUGATE % 1f;
        if (isVerticalGate)
        {
            var offset = Position.X - startPosition.X > 0 ? -32 : 32;
            return new Vector2(Position.X + offset, (float)(Position.Y - 80 + (spread * gateLength)));
        }
        else
        {
            var offset = Position.Y - startPosition.Y > 0 ? -32 : 32;
            return new Vector2((float)(Position.X - 80 + (spread * gateLength)), Position.Y + offset);

        }
    }
    private void DisableArea()
    {
        foreach (var enemy in DamageMap.Keys)
        {
            freeEnemy(enemy);
        }
        damageCount = 0;
        base.SetCollisionMaskValue(2, false);
        base.SetCollisionMaskValue(3, false);
    }
    private void OnDamageTimerTimeout()
    {
        health -= damageCount;
        label.Text = health.ToString() + "\n" + damageCount;
        if (health <= 0)
        {
            DisableArea();
        }
    }
}
