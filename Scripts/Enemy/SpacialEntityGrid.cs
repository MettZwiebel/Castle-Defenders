using Godot;
using System;

public class SpatialEntityGrid
{
    private int[] _head;
    private int[] _next;
    private Vector2I dimensions;

    public SpatialEntityGrid(Vector2I Dimensions, uint maxEnemies)
    {
        this.dimensions = Dimensions;
        _head = new int[dimensions.X * dimensions.Y];
        _next = new int[maxEnemies];

        Array.Fill(_head, -1);
        Array.Fill(_next, -1);
    }

    private int GetGridIndex(int x, int y) => (y * dimensions.X) + x;

    public void Resize(Vector2I Dimensions, uint maxEnemies)
    {
        this.dimensions = Dimensions;
        _head = new int[dimensions.X * dimensions.Y];
        _next = new int[maxEnemies];
        Array.Fill(_head, -1);
        Array.Fill(_next, -1);
    }

    public void MoveEntity(int entityIdx, Vector2I oldCell, Vector2I newCell)
    {
        // 1. Remove from old cell
        int oldIdx = GetGridIndex(oldCell.X, oldCell.Y);
        Remove(oldIdx, entityIdx);

        // 2. Add to new cell
        int newIdx = GetGridIndex(newCell.X, newCell.Y);
        _next[entityIdx] = _head[newIdx];
        _head[newIdx] = entityIdx;
    }

    private void Remove(int gridIdx, int entityIdx)
    {
        int current = _head[gridIdx];
        int prev = -1;

        while (current != -1)
        {
            if (current == entityIdx)
            {
                if (prev == -1) _head[gridIdx] = _next[current];
                else _next[prev] = _next[current];

                _next[current] = -1;
                return;
            }
            prev = current;
            current = _next[current];
        }
    }

    public void AddToCell(int entityIdx, Vector2I cell)
    {
        int gridIdx = GetGridIndex(cell.X, cell.Y);

        var lol = _head[gridIdx];
        _next[entityIdx] = lol;
        _head[gridIdx] = entityIdx;
    }

    public void RemoveFromCell(int entityIdx, Vector2I cell)
    {
        int gridIdx = GetGridIndex(cell.X, cell.Y);

        int current = _head[gridIdx];
        int prev = -1;

        while (current != -1)
        {
            if (current == entityIdx)
            {
                if (prev == -1) _head[gridIdx] = _next[current];
                else _next[prev] = _next[current];

                _next[current] = -1;
                return;
            }
            prev = current;
            current = _next[current];
        }
    }

    public int GetFirstInCell(Vector2I vec)
    {
        if (vec.X < 0 || vec.X >= dimensions.X || vec.Y < 0 || vec.Y >= dimensions.Y) return -1;
        return _head[GetGridIndex(vec.X, vec.Y)];
    }

    public int GetNext(int currentEntityIdx) => _next[currentEntityIdx];
}
