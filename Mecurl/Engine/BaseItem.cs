using BearLib;
using Engine.Drawing;
using System.Drawing;

namespace Engine
{
    public abstract class BaseItem : IDrawable
    {
        public string Name { get; }
        public Color Color { get; }
        public char Symbol { get; }

        public Loc Pos { get; set; }
        public bool ShouldDraw { get; set; }

        public BaseItem(in Loc pos, string name, Color color, char symbol)
        {
            Name = name;
            Pos = pos;
            Color = color;
            Symbol = symbol;
            ShouldDraw = true;
        }

        public void Draw(LayerInfo layer)
        {
            if (!ShouldDraw)
                return;

            Terminal.Color(Color);
            layer.Put(Pos.X - Camera.X, Pos.Y - Camera.Y, Symbol);
        }
    }
}
