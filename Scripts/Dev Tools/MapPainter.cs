using Godot;
using System;

public partial class MapPainter : Node2D
{

    [Export]
    TileMapLayer Ground, Gates, Walls;


    public override void _Ready()
    {
        MapHandler mapHandler = new MapHandler(Walls, Gates, Ground, 100, 100);
        BattleHandler battleHandler = new BattleHandler(new CastleDefender.Scripts.World.FieldHandler(100, 100));
        GameContext Context = new GameContext(new Vector2I(100, 100), mapHandler, battleHandler);

        Context.Save();
    }

}
