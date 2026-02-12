using System.Dynamic;
using Godot;



public class WeaponStats
{
    public ProjectileType ProjectileType { get; set; }
    public string ProjectileSpritePath { get; set; } = "";
    public float Speed { get; set; } = 100;
    public float Size { get; set; } = 1;
    public float Firerate { get; set; } = 1;


    public WeaponStats() { }
    public WeaponStats(ProjectileType projectileType, string projectileSpritePath, float speed, float size, float firerate)
    {
        this.ProjectileType = projectileType;
        this.ProjectileSpritePath = projectileSpritePath;
        this.Speed = speed;
        this.Size = size;
        this.Firerate = firerate;


    }
}

public enum ProjectileType
{
    RAYCAST,
    SHAPECAST,
    BALLISTIC
}
