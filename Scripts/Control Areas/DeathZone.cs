using Godot;
using Godot.Collections;

public partial class DeathZone : Area2D
{
    [Export]
    public CollisionShape2D collider;
    private readonly Array<EnemyBody> toDamage = new Array<EnemyBody>();
    private readonly Array<int> toErase = new Array<int>();

    public void OnBodyEntered(Node2D body)
    {
        if (body is EnemyBody)
        {
            var enemy = (EnemyBody)body;
            toDamage.Add(enemy);
        }
    }
    public void OnBodyExited(Node2D body)
    {
        if (body is EnemyBody)
        {
            var enemy = (EnemyBody)body;
            toDamage.Remove(enemy);
        }
    }

    public void OnTimeout()
    {
        for (int i = 0; i < toDamage.Count; i++)
        {
            if (toDamage[i] == null)
            {
                toErase.Add(i);
                continue;
            }
            toDamage[i].takeDamage(1000);
        }
        if (toErase.Count > 0)
        {
            for (int i = toErase.Count - 1; i >= 0; i--)
            {
                toDamage.RemoveAt(toErase[i]);
            }
            toErase.Clear();
        }
    }
}
