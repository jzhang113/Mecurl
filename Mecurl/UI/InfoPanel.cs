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
        private static readonly Color brightGreen = Color.FromArgb(48, 238, 48);
        private static readonly Color golden = Color.FromArgb(217, 163, 0);
        private static readonly TileMap placeholder;

        static InfoPanel()
        {
            var reader = new RexReader("AsciiArt/placeholder.xp");
            placeholder = reader.GetMap();
        }

        public static void Draw(LayerInfo layer)
        {
            // draw borders
            Terminal.Color(Colors.BorderColor);
            layer.DrawBorders(new BorderInfo
            {
                TopLeftChar = '┌',
                BottomLeftChar = '└',
                BottomRightChar = '┴',
                TopChar = '─', // 196
                BottomChar = '─',
                LeftChar = '│' // 179
            });

            var player = (Mech)Game.Player;
            int y = 1;

            Terminal.Color(Colors.Text);
            layer.Print(y, "snake153 the Pilot");
            y += 2;

            Terminal.Color(golden);
            Terminal.Layer(1);
            DrawBar(layer, 0, y, layer.Width, 0, '░');

            Terminal.Layer(2);
            layer.Print(y, "Energy");
            y++;

            Terminal.Color(Color.LightSkyBlue);
            Terminal.Layer(1);
            DrawBar(layer, 0, y, layer.Width, 0, '░');

            Terminal.Layer(2);
            layer.Print(y, "Coolant");
            y++;

            Terminal.Color(Color.DarkOrange);
            Terminal.Layer(4);
            layer.Print(y, "Heat");

            DrawHeatBar(layer, 4, y, player);
            y += 2;

            Terminal.Color(Colors.Text);
            foreach (Part p in player.PartHandler)
            {
                // Ignore weapons for now - we draw them with their weapon groups
                if (p is Weapon) continue;

                Terminal.Color(Colors.Text);
                Terminal.Layer(1);
                DrawBar(layer, 1, y, layer.Width - 1, 0, '░');

                Terminal.Layer(2);
                layer.Print(1, y++, p.Name);

                y++;
                y = DrawPart(layer, y, p);
                y++;
            }

            y++;
            for (int i = 0; i < player.WeaponGroup.Groups.Length; i++)
            {
                List<Weapon> group = player.WeaponGroup.Groups[i];
                if (group.Count == 0) continue;

                Terminal.Color(golden);
                layer.Print(y++, $"Weapon Group {i + 1}");
                layer.Print(y++, $"────────────────────");
                var currWeaponIndex = player.WeaponGroup.NextIndex(i);

                for (int j = 0; j < group.Count; j++)
                {
                    // TODO: it's possible to run out of space, in which case we would need scrolling
                    // we just truncate and not worry about it for now
                    if (y + 6 > layer.Height) break;

                    Weapon w = group[j];
                    double cooldown = layer.Width - layer.Width * w.CurrentCooldown / w.Cooldown;

                    if (w.CurrentCooldown == 0)
                    {
                        Terminal.Color(brightGreen);
                    }
                    else
                    {
                        Terminal.Color(Color.LightGreen.Blend(Color.LightSalmon, 1 - cooldown / layer.Width));
                    }

                    if (currWeaponIndex == j)
                    {
                        layer.Put(0, y, 0xE011);
                    }

                    Terminal.Layer(1);
                    DrawBar(layer, 1, y, cooldown - 1, 0, '░');

                    Terminal.Layer(2);
                    layer.Print(1, y++, w.Name);
                    Terminal.Layer(1);

                    y++;
                    y = DrawPart(layer, y, w);
                    y++;
                }
            }
        }

        private static int DrawPart(LayerInfo layer, int y, Part p)
        {
            //Terminal.Color(Color.White);
            layer.Print(1, y, $"Stab:{(int)(p.Stability / p.MaxStability * 100)}%");

            if (p.Cooldown > 0)
            {
                layer.Print(1, y + 1, $"Rchg:{p.CurrentCooldown}");
            }
            else
            {
                layer.Print(1, y + 1, $"Rchg:-");
            }

            if (p is Weapon)
            {
                layer.Print(1, y + 2, $"Ammo:{1000}");
            }
            else
            {
                layer.Print(1, y + 2, $"Ammo:-");
            }

            TileMap img = p.Art ?? placeholder;
            DrawTileMap(layer, layer.Width - img.Width, y, img);
            y += img.Height;

            return y;
        }

        private static void DrawTileMap(LayerInfo layer, int x, int y, TileMap tileMap)
        {
            for (int ax = 0; ax < tileMap.Width; ax++)
            {
                for (int ay = 0; ay < tileMap.Height; ay++)
                {
                    var tile = tileMap.Layers[0].Tiles[ay, ax];
                    Color color = Color.FromArgb(tile.ForegroundRed, tile.ForegroundGreen, tile.ForegroundBlue);

                    Terminal.Layer(1);
                    Terminal.Color(color);
                    layer.Put(x + ax, ay + y, CharTranslation.ToUnicode(tile.CharacterCode));
                }
            }
        }

        // HACK: this would be way easier with a real rectangle drawing API
        // still breaks on some parameters, but I don't know which
        private static void DrawHeatBar(LayerInfo layer, int x, int y, Mech player)
        {
            double barLength = layer.Width - x;
            double quarterBar = barLength / 4;

            double currentLength = player.CurrentHeat / player.PartHandler.TotalHeatCapacity * quarterBar * 3;

            var lowColor = Color.FromArgb(219, 226, 175);
            var medColor = Color.FromArgb(237, 217, 110);
            var highColor = Color.FromArgb(207, 112, 71);
            var dangerColor = Color.FromArgb(122, 0, 45);
            var regionColor = new Color[] { Colors.Background, lowColor, medColor, highColor, dangerColor };

            // fix the border
            //Terminal.Color(Colors.BorderColor);
            //Terminal.Layer(4);
            //layer.Put(-1, y, '│');

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
                DrawBar(layer, x + startPos, y, segmentLength, startFrac, tile);

                if ((int)segmentLength == 0)
                {
                    if (startFrac > 0)
                    {
                        Terminal.Color(regionColor[i]);
                        Terminal.Layer(3);
                        Terminal.PutExt(layer.X + x + startPos, layer.Y + y, (int)((startFrac - 1) * Terminal.State(Terminal.TK_CELL_WIDTH)), 0, tile);
                        Terminal.Layer(1);
                    }
                    else
                    {
                        Terminal.Color(regionColor[i]);
                        Terminal.Layer(3);
                        Terminal.Put(layer.X + x + startPos - 1, layer.Y + y, tile);
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
