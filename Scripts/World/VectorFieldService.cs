using System;
using System.Collections.Generic;

using CastleDefender.Scripts.World;
using Godot;

public class VectorFieldService
{
    private Vector2I MapToVectorsFactor = new Vector2I(2, 2);
    private FieldCell[,] VectorField;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public const int WallWeight = 500;
    public const int WallAvoidanceWeight = 100;
    public const int GateWeight = 10;
    public bool Debug { get; set; }
    public Node2D DebugTree { get; private set; } = new Node2D();
    private Vector2I Target = new Vector2I(0, 0);
    private Godot.Collections.Array<Vector2I> Gates, Walls;
    public VectorFieldService(int Width, int Height, Godot.Collections.Array<Vector2I> Gates, Godot.Collections.Array<Vector2I> Walls, bool Debug)
    {
        this.Width = Width;
        this.Height = Height;
        this.Gates = Gates;
        this.Walls = Walls;
        this.Debug = Debug;
        VectorField = new FieldCell[Width, Height];
        for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
            {
                VectorField[i, j] = new FieldCell(new Vector2I(i, j), 1);
            }
        InstantiationPass();

    }

    public void UpdateMap(int[,] Weights, Godot.Collections.Array<Vector2I> Gates, Godot.Collections.Array<Vector2I> Walls)
    {
        foreach (var cell in VectorField)
            cell.StaticWeight = Weights[cell.Position.X, cell.Position.Y];
        this.Walls = Walls;
        this.Gates = Gates;
        InstantiationPass();
    }

    public Vector2[,] Calculate(Vector2I Target)
    {
        this.Target = Target;
        PreparationPass();
        IntegrationPass();
        VectorPass();
        PostProcessingPass();
        //FinalDeadzonePass();
        if (Debug)
            DebugPass();

        var vectors = new Vector2[Width, Height];
        foreach (var cell in VectorField)
            vectors[cell.Position.X, cell.Position.Y] = cell.Direction;
        return vectors;
    }

    private void InstantiationPass()
    {
        foreach (var cellVec in Walls)
        {
            if (!isInBounds(cellVec))
                continue;

            VectorField[cellVec.X, cellVec.Y].StaticWeight = WallWeight;
            foreach (var neighborVec in neighborDirections)
                if (isInBounds(neighborVec + cellVec) && VectorField[neighborVec.X + cellVec.X, neighborVec.Y + cellVec.Y].StaticWeight != WallWeight)
                    VectorField[neighborVec.X + cellVec.X, neighborVec.Y + cellVec.Y].StaticWeight = WallAvoidanceWeight;
        }
        foreach (var cellVec in Gates)
        {
            if (!isInBounds(cellVec))
                continue;
            VectorField[cellVec.X, cellVec.Y].StaticWeight = GateWeight;
            foreach (var neighborDir in neighborCardinalDirections)
            {
                var neighborVec = neighborDir + cellVec;
                if (!isInBounds(neighborVec))
                    continue;
                if (VectorField[neighborVec.X, neighborVec.Y].StaticWeight == WallWeight)
                {
                    VectorField[cellVec.X, cellVec.Y].StaticWeight = WallAvoidanceWeight;
                    continue;
                }
                if (VectorField[neighborVec.X, neighborVec.Y].StaticWeight != GateWeight && VectorField[neighborVec.X, neighborVec.Y].StaticWeight != WallAvoidanceWeight)
                    VectorField[neighborVec.X, neighborVec.Y].StaticWeight = 1;
            }
        }

    }
    private void PreparationPass()
    {
        foreach (var cell in VectorField)
        {
            cell.CalculatedWeight = int.MaxValue;
        }
    }

    private void IntegrationPass()
    {
        if (!isInBounds(Target))
            return;
        LinkedList<Vector2I> toProcess = new LinkedList<Vector2I>();
        VectorField[Target.X, Target.Y].CalculatedWeight = 0;
        toProcess.AddFirst(Target);

        while (toProcess.Count > 0)
        {
            var cellVec = toProcess.First.Value;
            toProcess.RemoveFirst();


            foreach (var neighborDir in neighborDirections)
            {
                var neighborVec = cellVec + neighborDir;
                if (!isInBounds(neighborVec))
                    continue;
                var travelCost = VectorField[cellVec.X, cellVec.Y].CalculatedWeight + VectorField[neighborVec.X, neighborVec.Y].StaticWeight;
                if (travelCost < VectorField[neighborVec.X, neighborVec.Y].CalculatedWeight)
                {
                    VectorField[neighborVec.X, neighborVec.Y].CalculatedWeight = travelCost;
                    toProcess.AddLast(neighborVec);
                }
            }
        }
    }

    private void VectorPass()
    {
        foreach (var cell in VectorField)
        {
            var cellVec = cell.Position;
            var avgDirection = Vector2.Zero;
            var lowestWeight = int.MaxValue;
            if (cell.StaticWeight == WallWeight)
            {
                cell.Direction = ((Vector2)cell.Position).DirectionTo(Target) * -1;
                continue;
            }
            foreach (var neighborDir in neighborDirections)
            {
                var neighborVec = cellVec + neighborDir;
                if (!isInBounds(neighborVec))
                    continue;
                if (VectorField[neighborVec.X, neighborVec.Y].CalculatedWeight < lowestWeight)
                {
                    avgDirection = Vector2.Zero;
                    avgDirection += neighborDir;
                    lowestWeight = VectorField[neighborVec.X, neighborVec.Y].CalculatedWeight;
                    continue;
                }
                if (VectorField[neighborVec.X, neighborVec.Y].CalculatedWeight == lowestWeight)
                    avgDirection += neighborDir;
            }
            cell.Direction = avgDirection.Normalized();
        }
    }

    private void DebugPass()
    {
        DebugTree.QueueFree();
        DebugTree = new Node2D();
        var vectorScene = GD.Load<PackedScene>("res://Scenes/Debug/vector.tscn");
        float scaleX = 1f / MapToVectorsFactor.X;
        float scaleY = 1f / MapToVectorsFactor.Y;
        var scale = new Vector2(scaleX, scaleY); ;
        GD.Print("In Debug, Scale: " + scale);
        foreach (var cell in VectorField)
        {
            /*
                        //cell.CalculatedWeight > 999 ? "999" : cell.CalculatedWeight.ToString()) + "\n" +
                        var label = new Label();
                        label.Position = cell.Position * 16;
                        label.Text = cell.StaticWeight.ToString();
                        label.LabelSettings = new LabelSettings();
                        label.LabelSettings.FontSize = 8;
                        label.Visible = true;
                        DebugTree.AddChild(label);
            */
            if (cell.Direction == Vector2.Zero)
                continue;
            var degrees = (Math.Atan2(cell.Direction.Y * -1, cell.Direction.X * -1) * (180 / Math.PI)) - 90;

            var vector = (Node2D)vectorScene.Instantiate();
            vector.Scale = scale;
            vector.Position = new Vector2(cell.Position.X * 16 + 8, cell.Position.Y * 16 + 8);
            vector.RotationDegrees = (float)degrees;
            DebugTree.AddChild(vector);
        }
    }

    private void PostProcessingPass()
    {
        foreach (var cell in VectorField)
        {
            if (cell.StaticWeight == WallWeight || cell.StaticWeight == GateWeight || cell.CalculatedWeight == 0)
                continue;
            var directions = extendedNeighborDirections;
            if (cell.StaticWeight == WallAvoidanceWeight)
                directions = neighborCardinalDirections;
            var avgDirection = cell.Direction;
            bool isZero = false;
            if (cell.Direction == Vector2.Zero)
                isZero = true;
            foreach (var neighborVec in directions)
            {
                var pos = cell.Position + neighborVec;
                if (!isInBounds(pos) || VectorField[pos.X, pos.Y].StaticWeight == WallWeight)
                    continue;
                avgDirection += VectorField[pos.X, pos.Y].Direction;
            }
            if (isZero)
                cell.Direction = avgDirection.Normalized().Rotated(1.57f);
            else
                cell.Direction = avgDirection.Normalized();
        }
    }

    private void FinalDeadzonePass()
    {
        foreach (var cell in VectorField)
        {
            if (cell.StaticWeight == WallWeight || cell.StaticWeight == GateWeight)
                continue;
            var vec = new Vector2I((int)Math.Round(cell.Direction.X), (int)Math.Round(cell.Direction.Y));
            var neighborVec = cell.Position + vec;
            if (!isInBounds(neighborVec))
                continue;
            var neighborCell = VectorField[neighborVec.X, neighborVec.Y];
            var dot = neighborCell.Direction.Dot(cell.Direction);
            while (dot < -0.8f)
            {
                cell.Direction = cell.Direction.Rotated(0.017f);
                dot = neighborCell.Direction.Dot(cell.Direction);
            }

        }
    }


    public Node2D GetDebugTree()
    {
        return DebugTree;
    }

    public FieldCell[,] GetFieldCells()
    {
        return VectorField;
    }

    public bool isInBounds(Vector2I target)
    {
        return target.X >= 0 && target.X < Width && target.Y >= 0 && target.Y < Height;
    }

    private readonly Vector2I[] neighborDirections = new Vector2I[]
        {
            new Vector2I(0,1),new Vector2I(1,1),new Vector2I(1,0),new Vector2I(1,-1),
            new Vector2I(0,-1), new Vector2I(-1,-1), new Vector2I(-1,0),new Vector2I(-1,1)
        };
    private readonly Vector2I[] extendedNeighborDirections = new Vector2I[]
        {   new Vector2I(-3,-3),new Vector2I(-2,-3),new Vector2I(-1,-3),new Vector2I(0,-3),new Vector2I(1,-3),new Vector2I(2,-3),new Vector2I(3,-3),
            new Vector2I(-3,-2),new Vector2I(-2,-2),new Vector2I(-1,-2),new Vector2I(0,-2),new Vector2I(1,-2),new Vector2I(2,-2),new Vector2I(3,-2),
            new Vector2I(-3,-1),new Vector2I(-2,-1),new Vector2I(-1,-1),new Vector2I(0,-1),new Vector2I(1,-1),new Vector2I(2,-1),new Vector2I(3,-1),
            new Vector2I(-3,0),new Vector2I(-2,0),new Vector2I(-1,0),                     new Vector2I(1,0),new Vector2I(2,0),new Vector2I(3,0),
            new Vector2I(-3,1),new Vector2I(-2,1),new Vector2I(-1,1),new Vector2I(0,1),new Vector2I(1,1),new Vector2I(2,1),new Vector2I(3,1),
            new Vector2I(-3,2),new Vector2I(-2,2),new Vector2I(-1,2),new Vector2I(0,2),new Vector2I(1,2),new Vector2I(2,2),new Vector2I(3,2),
            new Vector2I(-3,3),new Vector2I(-2,3),new Vector2I(-1,3),new Vector2I(0,3),new Vector2I(1,3),new Vector2I(2,3),new Vector2I(3,3),
        };
    private readonly Vector2I[] neighborCardinalDirections = new Vector2I[]
        {
            new Vector2I(0,1),new Vector2I(1,0),new Vector2I(0,-1), new Vector2I(-1,0)
        };
}