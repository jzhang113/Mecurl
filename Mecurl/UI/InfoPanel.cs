﻿using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Parts;
using System;
using System.Collections.Generic;

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
                // Ignore weapons for now - we draw them with their weapon groups
                if (p is Weapon) continue;

                layer.Print(y++, p.Name);
                int barLength = (int)(p.MaxHealth / 10);
                int remainLength = Math.Max((int)(p.Health / 10), 0);
                string healthString =
                    "[color=red]" + new String('|', remainLength) + "[/color]" +
                    "[color=gray]" + new string('|', barLength - remainLength) + "[/color]";
                layer.Print(y++, $"[[{healthString}]]");
                y++;
            }

            y++;
            for (int i = 0; i < player.WeaponGroup.Groups.Length; i++)
            {
                List<Weapon> group = player.WeaponGroup.Groups[i];
                if (group.Count == 0) continue;

                layer.Print(y++, $"Weapon Group {i+1}");
                var currWeaponIndex = player.WeaponGroup.NextIndex(i);

                for (int j = 0; j < group.Count; j++)
                {
                    Weapon w = group[j];
                    if (currWeaponIndex == j)
                    {
                        layer.Put(-1, y, '>');
                    }

                    layer.Print(y++, w.Name);
                    int barLength = (int)(w.MaxHealth / 10);
                    int remainLength = Math.Max((int)(w.Health / 10), 0);
                    string healthString =
                        "[color=red]" + new String('|', remainLength) + "[/color]" +
                        "[color=gray]" + new string('|', barLength - remainLength) + "[/color]";
                    layer.Print(y++, $"[[{healthString}]]");
                    y++;
                }
            }
        }
    }
}
