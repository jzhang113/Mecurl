using BearLib;
using Engine.Drawing;
using System.Drawing;

namespace Engine.Map
{
    public enum TileType
    {
        Wall, Ground, Debris, Grass
    }

    public class Tile
    {
        public int X { get; }
        public int Y { get; }
        public Color Color { get; internal set; }
        public char Symbol { get; private set; }
        public TileType Terrain
        {
            get => _terrain;
            internal set
            {
                _terrain = value;
                Symbol = _terrain switch
                {
                    TileType.Wall => '#',
                    TileType.Grass => '"',
                    TileType.Ground => '.',
                    TileType.Debris => GetRubbleSymbol(),
                    _ => ' ',
                };
            }
        }

        public float Light
        {
            get => _light;
            internal set
            {
                if (value < 0)
                    _light = 0;
                else if (value > 1)
                    _light = 1;
                else
                    _light = value;
            }
        }

        public int Fuel { get; internal set; }
        public bool IsOccupied { get; internal set; }
        public bool IsExplored { get; internal set; }
        public bool BlocksLight { get; internal set; }
        public bool LosExists { get; internal set; }

        public bool IsVisible => LosExists && Light > EngineConsts.MIN_VISIBLE_LIGHT_LEVEL;
        public bool IsWall => Terrain == TileType.Wall;
        public bool IsWalkable => !IsWall && !IsOccupied;
        public bool IsLightable => !IsWall && !BlocksLight;

        private float _light;
        private TileType _terrain;

        public Tile(int x, int y, in Color color)
        {
            X = x;
            Y = y;
            Color = color;
            Terrain = TileType.Wall;
        }

        public void Draw(LayerInfo layer)
        {
            int dispX = X - Camera.X;
            int dispY = Y - Camera.Y;

            Terminal.Color(Color);
            layer.Put(dispX, dispY, Symbol);
        }

        private static char GetRubbleSymbol()
        {
            double rubble = BaseGame.VisRand.NextDouble();
            if (rubble < 0.07)
            {
                return '~';
            }
            if (rubble < 0.3)
            {
                return '`';
            }
            if (rubble < 0.5)
            {
                return ';';
            }
            else if (rubble < 0.8)
            {
                return ',';
            }
            else
            {
                return '.';
            }
        }
    }
}