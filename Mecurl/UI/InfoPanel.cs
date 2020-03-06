using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Actors;
using Mecurl.Parts;
using RexTools;
using System;
using System.Collections.Generic;
using System.Drawing;

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

            var player = (Actors.Mech)Game.Player;
            int y = 1;

            Terminal.Color(Colors.Text);
            layer.Print(y++, "Axel the Pilot");
            layer.Print(y++, "[[[color=blue]||||||||||||||[/color]]]");
            layer.Print(y++, "[[[color=purple]||||||||||||||[/color]]]");

            DrawHeatBar(layer, 0, y, player);
            y += 2;

            Terminal.Color(Colors.Text);
            foreach (Part p in player.PartHandler)
            {
                // Ignore weapons for now - we draw them with their weapon groups
                if (p is Weapon) continue;

                layer.Print(y++, p.Name);

                if (p.Art != null)
                {
                    DrawTileMap(layer, 1, y, p.Art);
                    y += p.Art.Height + 1;
                }
                else
                {
                    int barLength = (int)(p.MaxHealth / 10);
                    int remainLength = Math.Max((int)(p.Health / 10), 0);
                    string healthString =
                        "[color=red]" + new String('|', remainLength) + "[/color]" +
                        "[color=gray]" + new string('|', barLength - remainLength) + "[/color]";
                    layer.Print(y++, $"[[{healthString}]]");
                }

                y++;
            }

            y++;
            for (int i = 0; i < player.WeaponGroup.Groups.Length; i++)
            {
                List<Weapon> group = player.WeaponGroup.Groups[i];
                if (group.Count == 0) continue;

                layer.Print(y++, $"Weapon Group {i + 1}");
                layer.Print(y++, $"────────────────────");
                var currWeaponIndex = player.WeaponGroup.NextIndex(i);

                for (int j = 0; j < group.Count; j++)
                {
                    Weapon w = group[j];
                    if (currWeaponIndex == j)
                    {
                        Terminal.Color(Color.FromArgb(48, 238, 48));
                        layer.Put(0, y, 0xE011);
                    }

                    double cooldown = layer.Width - layer.Width * w.CurrentCooldown / w.Cooldown;

                    Terminal.Layer(1);
                    Terminal.Color(Color.LightGreen.Blend(Color.LightSalmon, 1 - cooldown / layer.Width));
                    DrawBar(layer, 1, y, cooldown, 0, '░');
                    Terminal.Layer(2);

                    if (w.CurrentCooldown == 0) Terminal.Color(Color.FromArgb(48, 238, 48));
                    layer.Print(1, y++, w.Name);
                    Terminal.Layer(1);

                    y++;

                    if (w.Art != null)
                    {
                        DrawTileMap(layer, 2, y, w.Art);
                        y += w.Art.Height + 1;
                    }
                    else
                    {
                        int barLength = (int)(w.MaxHealth / 10);
                        int remainLength = Math.Max((int)(w.Health / 10), 0);
                        string healthString =
                            "[color=red]" + new String('|', remainLength) + "[/color]" +
                            "[color=gray]" + new string('|', barLength - remainLength) + "[/color]";
                        layer.Print(y++, $"[[{healthString}]]");
                    }

                    Terminal.Color(Colors.Text);
                }
            }
        }

        private static void DrawTileMap(LayerInfo layer, int x, int y, TileMap tileMap)
        {
            for (int ax = 0; ax < tileMap.Width; ax++)
            {
                for (int ay = 0; ay < tileMap.Height; ay++)
                {
                    var tile = tileMap.Layers[0].Tiles[ay, ax];
                    Terminal.Color(Color.FromArgb(tile.ForegroundRed, tile.ForegroundGreen, tile.ForegroundBlue));
                    layer.Put(x + ax, ay + y, CharTranslation.ToUnicode(tile.CharacterCode));
                }
            }
        }

        // HACK: this would be way easier with a real rectangle drawing API
        // unitSize 4 breaks it, dunno why
        // everything from 1 - 7 works though
        private static void DrawHeatBar(LayerInfo layer, int x, int y, Mech player)
        {
            // how much should 1 block of heat represent
            const double unitSize = 2;            

            double barLength = player.PartHandler.TotalHeatCapacity / unitSize;
            double quarterBar = barLength / 4;

            double currentLength = player.CurrentHeat / unitSize;

            int xPos = x;
            var regionColor = new Color[] { Color.Black, Color.Green, Color.Yellow, Color.Orange, Color.Red };

            Terminal.Color(Color.White);
            Terminal.Layer(4);
            layer.Put(xPos++, y, '[');
            Terminal.PutExt(layer.X + xPos + (int)barLength, layer.Y + y, (int)((barLength - (int)barLength) * 12), 0, ']');
            Terminal.Layer(1);

            // draw 25% of the bar each iteration
            for (int i = 0; i < 4; i++)
            {
                // skip if we haven't reached this segment
                double segmentMin = quarterBar * i;
                if (segmentMin > currentLength) continue;

                // on the other hand, if we are past this segment, only consider this quarter
                double segmentMax = quarterBar * (i + 1);
                double segmentLength;
                if (currentLength < segmentMax)
                {
                    segmentLength = currentLength - segmentMin;
                }
                else
                {
                    segmentLength = quarterBar;
                }

                int startPos = (int)segmentMin;
                double startFrac = segmentMin - startPos;
                char tile = '█';

                Terminal.Color(regionColor[i + 1]);
                DrawBar(layer, x + 1 + startPos, y, segmentLength, startFrac, tile);

                if ((int)segmentLength == 0)
                {
                    if (startFrac > 0)
                    {
                        Terminal.Color(regionColor[i]);
                        Terminal.Layer(3);
                        Terminal.PutExt(layer.X + startPos + 1, layer.Y + y, (int)((startFrac - 1) * Terminal.State(Terminal.TK_CELL_WIDTH)), 0, tile);
                        Terminal.Layer(1);
                    }
                    else
                    {
                        Terminal.Color(regionColor[i]);
                        Terminal.Layer(3);
                        Terminal.Put(layer.X + startPos, layer.Y + y, tile);
                        Terminal.Layer(1);
                    }
                }
            }
        }

        private static void DrawBar(LayerInfo layer, int x, int y, double length, double dx, char c)
        {
            // number of tiles of the segment
            int regionTiles = (int)length;
            double remaining = length - regionTiles;

            int cellWidth = Terminal.State(Terminal.TK_CELL_WIDTH);
            int offset = (int)(cellWidth * dx);

            for (int j = 0; j < regionTiles; j++)
            {
                Terminal.PutExt(layer.X + x + j, layer.Y + y, offset, 0, c);
            }

            if (remaining > 0)
            {
                Terminal.Layer(2);
                Terminal.PutExt(layer.X + x + regionTiles - 1, layer.Y + y, offset + (int)(remaining * cellWidth), 0, c);
                Terminal.Layer(1);
            }
        }
    }
}
