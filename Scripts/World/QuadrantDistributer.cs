using Godot;
using System;
using Godot.Collections;
using System.Linq;

public static class QuadrantDistributer
{
    private const int SlotsPerQuadrant = 75;
    public static Vector2[] _templateSlots;
    private const float GoldenAngle = 2.39996322f;

    // Call once to create the "Template" for 1/4 of a circle
    public static void InitializeTemplate(float outerRadius)
    {
        _templateSlots = new Vector2[SlotsPerQuadrant];
        for (int i = 0; i < SlotsPerQuadrant; i++)
        {
            float t = (i + 0.5f) / SlotsPerQuadrant;
            float r = (float)Math.Sqrt(t) * outerRadius; // Base distribution
            float angle = (i * GoldenAngle) % (Mathf.Pi / 2); // Limit to 90 deg

            _templateSlots[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
    }

    public static Vector2[] GetAllPoints()
    {
        Array<Vector2> result = new Array<Vector2>();
        for (int i = 0; i < 4; i++)
        {
            foreach (var vector in _templateSlots)
            {
                var pos = vector;
                switch (i)
                {
                    case 1: pos = new Vector2(pos.Y, -pos.X); break;
                    case 2: pos = new Vector2(-pos.X, -pos.Y); break;
                    case 3: pos = new Vector2(-pos.Y, pos.X); break;
                }
                result.Add(pos);
            }
        }
        return result.ToArray();
    }

    public static Vector2 GetPosition(int uniqueId, Vector2 wallOffset, int[] activeQuadrants)
    {
        // 1. Pick a quadrant from the available list
        int quadIndex = activeQuadrants[uniqueId % activeQuadrants.Length];

        // 2. Pick a slot within that quadrant
        int slotIndex = uniqueId % SlotsPerQuadrant;
        Vector2 pos = _templateSlots[slotIndex];

        // 3. Rotate the template vector to the correct quadrant
        // 0: Top-Right, 1: Bottom-Right, 2: Bottom-Left, 3: Top-Left
        switch (quadIndex)
        {
            case 1: pos = new Vector2(pos.Y, -pos.X); break;
            case 2: pos = new Vector2(-pos.X, -pos.Y); break;
            case 3: pos = new Vector2(-pos.Y, pos.X); break;
        }

        // 4. Apply the wall thickness offset
        // Improvement: Each quadrant can have its own offset vector
        return pos + wallOffset;
    }
}
