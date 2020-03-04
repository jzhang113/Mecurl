using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Parts;
using System;

namespace Mecurl.UI
{
    internal static class InfoPanel
    {
        public static void Draw(LayerInfo layer)
        {
            // draw borders
            Terminal.Color(Colors.BorderColor);
            layer.DrawBorders(new BorderInfo
            {
                TopLeftChar = '┌',
                BottomLeftChar = '└',
                TopChar = '─', // 196
                BottomChar = '─',
                LeftChar = '│' // 179
            });

            var player = (Actors.Actor)Game.Player;
            int y = 1;

            layer.Print(y++, "Axel the Pilot");
            layer.Print(y++, "[[[color=blue]||||||||||||||[/color]]]");
            layer.Print(y++, "[[[color=purple]||||||||||||||[/color]]]");
            layer.Print(y++, "[[[color=orange]|             [/color]]]");

            y++;

            foreach (Part p in player.PartHandler)
            {
                layer.Print(y++, p.Name);
                int barLength = (int)(p.MaxHealth / 10);
                int remainLength = Math.Max((int)(p.Health / 10), 0);
                string healthString =
                    "[color=red]" + new String('|', remainLength) + "[/color]" +
                    "[color=gray]" + new string('|', barLength - remainLength) + "[/color]";
                layer.Print(y++, $"[[{healthString}]]");
                y++;
            }
        }
    }
}
