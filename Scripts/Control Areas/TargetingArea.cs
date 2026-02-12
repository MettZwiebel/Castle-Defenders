using System;
using System.Linq;
using CastleDefender.Scripts.World;
using Godot;
using Godot.Collections;

public partial class TargetingArea : Area2D
{
    public Tower parent;
    private Array<Vector2I> TargetCells = new Array<Vector2I>();
    private FieldCell[,] VectorField;
    private FieldHandler FieldHandler;
    private static readonly Dictionary<float, Array<Vector2I>> CoordinateCache = new Dictionary<float, Array<Vector2I>>();


    public void Prepare(float gridSize, float radius)
    {

        Array<Vector2I> cache = new Array<Vector2I>();
        foreach (var x in GetRelativeIntersectingCells(gridSize, radius))
        {
            var vector = x + FieldHandler.LocalToMap(parent.Position);
            cache.Add(vector);
        }
        var OrderdCells = cache.OrderBy(vector => VectorField[vector.X, vector.Y].CalculatedWeight).ToArray();
        foreach (var vector in OrderdCells)
        {
            TargetCells.Add(vector);
        }
    }
    public Vector2I? GetAttackTarget()
    {
        foreach (var vector in TargetCells)
        {
            if (VectorField[vector.X, vector.Y].EnemyCount > 0)
                return vector;
        }
        return null;
    }

    public static Array<Vector2I> GetRelativeIntersectingCells(float gridSize, float radius)
    {
        if (CoordinateCache.ContainsKey(radius))
        {
            return CoordinateCache[radius];
        }
        Array<Vector2I> result = new Array<Vector2I>();

        float gridRadius = radius / gridSize;
        float gridRadiusSquared = gridRadius * gridRadius;
        for (int i = 0; i <= (int)gridRadius; i++)
        {
            int distance = (int)Math.Sqrt(gridRadiusSquared - (i * i));
            for (int j = 0 - distance; j <= distance; j++)
            {
                result.Add(new Vector2I(j, i));
                if (i > 0)
                {
                    result.Add(new Vector2I(j, 0 - i));
                }
            }
        }
        CoordinateCache[radius] = result;
        return result;
    }
}
