using Engine;
using System.Drawing;

namespace Engine.Drawing
{
    public interface IDrawable
    {
        Loc Pos { get; set; }
        Color Color { get; }
        char Symbol { get; }

        bool ShouldDraw { get; set; }

        void Draw(LayerInfo layer);
    }
}
