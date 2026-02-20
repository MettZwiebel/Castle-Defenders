#nullable enable

using System;
using Godot;

namespace CastleDefender.Scripts.World;


public class FieldHandler
{
    public readonly Vector2I CellDimensions = new Vector2I(16, 16);
    public readonly Vector2I FieldDimensions;
    private Vector2[,] VectorField;
    private FieldCell[,] FieldCells;
    private uint[,] DensityField;

    public const int WallWeight = 500;

    public FieldHandler(int Width, int Height)
    {
        this.FieldDimensions = new Vector2I(Width, Height);
        VectorField = new Vector2[Width, Height];
        DensityField = new uint[Width, Height];
        FieldCells = new FieldCell[Width, Height];
    }

    public Vector2 GetDirectionAt(Vector2I coord)
    {
        var vec = VectorField[coord.X, coord.Y];
        return vec;
    }
    public uint GetDensityAt(Vector2I coord)
    {
        return DensityField[coord.X, coord.Y];
    }

    public FieldCell GetCellAt(Vector2I coords)
    {
        return FieldCells[coords.X, coords.Y];
    }

    public int GetWeightAt(Vector2I coords)
    {
        return FieldCells[coords.X, coords.Y].CalculatedWeight;
    }

    public Vector2 GetDirectionAt(Vector2 coord)
    {
        var pos = LocalToMap(coord);
        var vec = VectorField[pos.X, pos.Y];
        return vec;
    }

    public uint GetDensityAt(Vector2 coord)
    {
        var pos = LocalToMap(coord);
        return DensityField[pos.X, pos.Y];
    }

    public void UpdateVectorField(Vector2[,] vectors)
    {
        VectorField = vectors;
    }

    public void UpdateFieldCells(FieldCell[,] fieldCells)
    {
        this.FieldCells = fieldCells;
    }

    public Vector2I LocalToMap(Vector2 vec)
    {
        return new Vector2I((int)(vec.X / CellDimensions.X), (int)(vec.Y / CellDimensions.Y));
    }
    public Vector2 MapToLocal(Vector2I vec)
    {
        return new Vector2(vec.X * CellDimensions.X, vec.Y * CellDimensions.Y);
    }
    public void HandleCellTransition(Vector2I Position, Vector2I Origin)
    {
        DensityField[Position.X, Position.Y] += 1;

        DensityField[Origin.X, Origin.Y] -= 1;
    }

    public void IncreaseDensityAt(Vector2I Position)
    {
        DensityField[Position.X, Position.Y] += 1;
    }
    public void ReduceDensityAt(Vector2I Position)
    {
        DensityField[Position.X, Position.Y] -= 1;
    }

}
