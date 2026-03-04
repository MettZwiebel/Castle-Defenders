using Godot;

public struct ProjectileData
{
    //Lifecycle
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; set; }
    public float Speed { get; set; }
    public float DistanceTraveled { get; set; }
    public float MaxDistance { get; set; }
    public float Radius { get; set; }
    public bool IsAlive { get; set; }
    public int PenetrationCount { get; set; }
    public int HitCooldownFrames { get; set; }

    //Visuals
    public int SpriteIndex { get; set; }
    public uint EffectBitmask { get; set; }
    public Color Tint { get; set; }

    //Combat
    public int Health { get; set; }
    public int Damage { get; set; }


}
