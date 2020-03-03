using BearLib;
using Engine.Drawing;
using System.Drawing;

namespace Engine.Map
{
    public class Tile
    {
        public int X { get; }
        public int Y { get; }
        public Color Color { get; internal set; }
        public char Symbol { get; internal set; }

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
        public bool IsWall { get; set; }
        public bool IsWalkable => !IsWall && !IsOccupied;
        public bool IsLightable => !IsWall && !BlocksLight;

        private float _light;

        public Tile(int x, int y, in Color color)
        {
            X = x;
            Y = y;
            IsWall = true;
            Color = color;
            Symbol = '.';
        }

        public void Draw(LayerInfo layer)
        {
            int dispX = X - Camera.X;
            int dispY = Y - Camera.Y;
            Terminal.Color(Color);

            if (IsWall)
            {
                layer.Put(dispX, dispY, '#');
            }
            else
            {
                // Terminal.Color(Colors.Floor);
                layer.Put(dispX, dispY, Symbol);
            }
        }
    }
}