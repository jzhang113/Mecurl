using BearLib;
using Engine;
using Engine.Drawing;

namespace Mecurl.UI
{
    internal class ObjectivePanel
    {
        public static void Draw(LayerInfo layer)
        {
            // draw borders
            Terminal.Color(Colors.BorderColor);
            layer.DrawBorders(new BorderInfo
            {
                TopLeftChar = '├',
                TopRightChar = '┤',
                BottomLeftChar = '┴',
                BottomRightChar = '┘',
                TopChar = '─', // 196
                BottomChar = '─',
                RightChar = '│' // 179
            });

            Terminal.Color(Colors.Text);
            layer.Print(0, "Objective");
            layer.Print(1, "─────────");

            if (Game.NextMission.MissionType == MissionType.Elim)
            {
                layer.Print(2, $" {Game.MapHandler.Units.Count - 1} enemy remain");
            }
            else
            {
                layer.Print(2, " none");
            }
        }
    }
}
