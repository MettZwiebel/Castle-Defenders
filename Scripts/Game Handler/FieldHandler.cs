#nullable enable

using System;
using Godot;

namespace CastleDefender.Scripts.World;


public class FieldHandler
{
    private readonly Vector2I CellDimensions = new Vector2I(16, 16);
    private Vector2[,] VectorField;
    private EnemyBody?[,] EnemyField;
    private uint[,] DensityField;

    private int Width, Height;

    public FieldHandler(int Width, int Height)
    {
        this.Width = Width;
        this.Height = Height;
        VectorField = new Vector2[Width, Height];
        EnemyField = new EnemyBody[Width, Height];
        DensityField = new uint[Width, Height];
    }

    public Vector2 GetDirectionAt(Vector2I coord)
    {
        var vec = VectorField[coord.X, coord.Y];
        return vec;
    }
    public EnemyBody? GetEnemyAt(Vector2I coord)
    {
        return EnemyField[coord.X, coord.Y];
    }
    public uint GetDensityAt(Vector2I coord)
    {
        return DensityField[coord.X, coord.Y];
    }

    public void UpdateVectorField(Vector2[,] vectors)
    {
        VectorField = vectors;
    }

    public void EnterEnemyAt(Vector2I coords, EnemyBody body)
    {
        DensityField[coords.X, coords.Y]++;
        EnemyField[coords.X, coords.Y] = body;
    }
    public void ExitEnemyAt(Vector2I coords)
    {
        DensityField[coords.X, coords.Y]--;
        EnemyField[coords.X, coords.Y] = null;
    }

    public Vector2I LocalToMap(Vector2 vec)
    {
        return new Vector2I((int)(vec.X / CellDimensions.X), (int)(vec.Y / CellDimensions.Y));
    }
    public Vector2 MapToLocal(Vector2I vec)
    {
        return new Vector2(vec.X * CellDimensions.X, vec.Y * CellDimensions.Y);
    }
}
