using BearLib;
using Engine;
using Engine.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

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
                TopRightChar = '┐',
                BottomLeftChar = '┴',
                BottomRightChar = '┘',
                TopChar = '─', // 196
                BottomChar = '─',
                RightChar = '│' // 179
            });

            layer.Print(0, "Objective");
            layer.Print(1, "──────────");
        }
    }
}
