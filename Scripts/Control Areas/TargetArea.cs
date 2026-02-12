using Godot;



public abstract partial class TargetArea : Area2D
{
    public abstract void registerEnemy(EnemyBody body);
    public abstract void freeEnemy(EnemyBody body);
    public abstract void updateEnemy(EnemyBody body, bool isAttacking);
}