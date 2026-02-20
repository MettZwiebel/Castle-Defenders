using Godot;
using Godot.Collections;

public partial class MapHandler : Node2D
{
    public TileMapLayer Walls { get; private set; }
    public TileMapLayer Ground { get; private set; }
    public TileMapLayer Gates { get; private set; }

    private int Width, Height;


    private Dictionary<string, int> MapToMaster = new Dictionary<string, int>
    {
        {"gate",0},
        {"ground",1},
        {"wall",2}
    };
    private Dictionary<int, string> MasterToMap = new Dictionary<int, string>
    {
        {0,"gate"},
        {1,"ground"},
        {2,"wall"}
    };

    public MapHandler() { }

    public MapHandler(TileMapLayer Walls, TileMapLayer Gates, TileMapLayer Ground, int Width, int Height)
    {
        this.Walls = Walls;
        this.Gates = Gates;
        this.Ground = Ground;
        this.Width = Width;
        this.Height = Height;

    }

    public bool IsCellColliding(Vector2I coord)
    {
        return Walls.GetCellSourceId(coord) != -1 || Gates.GetCellSourceId(coord) != -1;
    }

    public void SetCellFromMaster(Vector2I coord, Vector2I atlas, int sid, int alt)
    {
        if (IsCellColliding(coord))
            return;
        switch (MasterToMap[sid])
        {
            case "gate":
                Gates.SetCell(coord, 0, atlas, alt);
                break;
            case "wall":
                Walls.SetCell(coord, 0, atlas, alt);
                break;
            case "ground":
                Ground.SetCell(coord, 0, atlas, alt);
                break;
        }
    }

    public void SetCellsFromMaster(Dictionary<Vector2I, Buildable> buildings)
    {
        foreach (var coord in buildings.Keys)
        {
            SetCellFromMaster(coord, buildings[coord].tileAtlas, buildings[coord].tileSid, buildings[coord].tileAlt);
        }
    }

    public Array<Vector2I> GetWalls()
    {
        return Walls.GetUsedCells();
    }

    public Array<Vector2I> GetGates()
    {
        return Gates.GetUsedCells();
    }

    public Array<Vector2I> GetMap()
    {
        return Ground.GetUsedCells();
    }

    public Array<Vector2I> GetScaledWalls(Vector2I scalar)
    {
        Array<Vector2I> result = new Array<Vector2I>();
        foreach (var wall in Walls.GetUsedCells())
        {
            result.AddRange(GetScaledCell(scalar, wall * scalar));
        }
        return result;
    }
    public Array<Vector2I> GetScaledGates(Vector2I scalar)
    {
        Array<Vector2I> result = new Array<Vector2I>();
        foreach (var gate in Gates.GetUsedCells())
        {
            result.AddRange(GetScaledCell(scalar, gate * scalar));
        }
        return result;
    }
    public Array<Vector2I> GetScaledGround(Vector2I scalar)
    {
        Array<Vector2I> result = new Array<Vector2I>();
        foreach (var ground in Ground.GetUsedCells())
        {
            result.AddRange(GetScaledCell(scalar, ground * scalar));
        }
        return result;
    }
    private Array<Vector2I> GetScaledCell(Vector2I scalar, Vector2I at)
    {
        Array<Vector2I> result = new Array<Vector2I>();
        for (int i = 0; i < scalar.X; i++)
        {
            for (int j = 0; j < scalar.Y; j++)
            {
                result.Add(new Vector2I(at.X + i, at.Y + j));
            }
        }
        return result;
    }
    public int[,] GetWeights()
    {
        int[,] result = new int[Width, Height];
        foreach (var coord in Ground.GetUsedCells())
            result[coord.X, coord.Y] = (int)Ground.GetCellTileData(coord).GetCustomData("Weight");
        foreach (var coord in Walls.GetUsedCells())
            result[coord.X, coord.Y] = (int)Ground.GetCellTileData(coord).GetCustomData("Weight");
        foreach (var coord in Gates.GetUsedCells())
            result[coord.X, coord.Y] = (int)Ground.GetCellTileData(coord).GetCustomData("Weight");

        return result;
    }

    public Dictionary<string, Variant> Save()
    {
        return new Dictionary<string, Variant>()
        {
            {"wid",Width},
            {"hei",Height},
            {"wal",SaveTilemapLayer(Walls)},
            {"gat",SaveTilemapLayer(Gates)},
            {"gro",SaveTilemapLayer(Ground)}
        };
    }

    public void Load(Dictionary<string, Variant> dict)
    {
        foreach (var key in dict.Keys)
        {
            switch (key)
            {
                case "wid":
                    this.Width = dict[key].As<int>();
                    break;
                case "hei":
                    this.Height = dict[key].As<int>();
                    break;
                case "wal":
                    this.Walls = LoadTilemapLayer(dict[key].As<Dictionary<string, Variant>>());
                    break;
                case "gat":
                    this.Gates = LoadTilemapLayer(dict[key].As<Dictionary<string, Variant>>());
                    break;
                case "gro":
                    this.Ground = LoadTilemapLayer(dict[key].As<Dictionary<string, Variant>>());
                    break;
                default:
                    break;
            }
        }
        base.AddChild(Ground);
        base.AddChild(Walls);
        base.AddChild(Gates);
    }
    public static Dictionary<string, Variant> SaveTilemapLayer(TileMapLayer layer)
    {
        return new Dictionary<string, Variant>()
        {
            {"pth",layer.TileSet.ResourcePath},
            {"cls",SaveTilemapData(layer)}
        };
    }
    public static Array<Dictionary<string, Variant>> SaveTilemapData(TileMapLayer layer)
    {
        Array<Dictionary<string, Variant>> layerData = new Array<Dictionary<string, Variant>>();
        foreach (var coords in layer.GetUsedCells())
        {
            layerData.Add(new Dictionary<string, Variant>()
            {
                {"poX", coords.X},
                {"poY",coords.Y},
                {"atX",layer.GetCellAtlasCoords(coords).X},
                {"atY", layer.GetCellAtlasCoords(coords).Y},
                {"sid",layer.GetCellSourceId(coords)},
                {"alt",layer.GetCellAlternativeTile(coords)}
            });
        }
        return layerData;
    }
    public static TileMapLayer LoadTilemapLayer(Dictionary<string, Variant> dict)
    {
        TileMapLayer layer = new TileMapLayer();

        foreach (var key in dict.Keys)
        {
            GD.Print("Loading " + key);
            switch (key)
            {
                case "pth":
                    layer.TileSet = GD.Load<TileSet>(dict[key].As<string>());
                    break;
                case "cls":
                    layer = LoadTilemapData(layer, dict[key].As<Array<Dictionary<string, Variant>>>());
                    break;
            }
        }
        return layer;
    }

    public static TileMapLayer LoadTilemapData(TileMapLayer layer, Array<Dictionary<string, Variant>> arr)
    {
        foreach (var cell in arr)
        {
            var poX = 0;
            var poY = 0;
            var atX = 0;
            var atY = 0;
            var sid = 0;
            var alt = 0;
            foreach (var key in cell.Keys)
                switch (key)
                {
                    case "poX":
                        poX = cell[key].As<int>();
                        break;
                    case "poY":
                        poY = cell[key].As<int>();
                        break;
                    case "atX":
                        atX = cell[key].As<int>();
                        break;
                    case "atY":
                        atY = cell[key].As<int>();
                        break;
                    case "sid":
                        sid = cell[key].As<int>();
                        break;
                    case "alt":
                        alt = cell[key].As<int>();
                        break;
                }
            layer.SetCell(new Vector2I(poX, poY), sid, new Vector2I(atX, atY), alt);
        }
        return layer;
    }

    public Vector2I LocalToMap(Vector2 vec)
    {
        return new Vector2I((int)(vec.X / Walls.TileSet.TileSize.X), (int)(vec.Y / Walls.TileSet.TileSize.Y));
    }
    public Vector2 MapToLocal(Vector2I vec)
    {
        return new Vector2(vec.X * Walls.TileSet.TileSize.X, vec.Y * Walls.TileSet.TileSize.Y);
    }

    public bool IsWallAt(Vector2 Position)
    {
        return Walls.GetCellSourceId(LocalToMap(Position)) != -1;
    }
}
