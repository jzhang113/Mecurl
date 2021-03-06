﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace Engine
{
    internal static class Colors
    {
        public static Color FloorBackground = Swatch.SecondaryDarkest;
        public static Color Floor = Swatch.Secondary;

        public static Color WallBackground = Swatch.SecondaryDarkest;
        public static Color Wall = Swatch.DbStone;

        // Units
        public static Color Player = Color.Wheat;

        // UI
        public static Color Path = Swatch.Alternate;
        public static Color Target = Swatch.PrimaryDarker;
        public static Color TargetBackground = Swatch.Secondary;
        public static Color Cursor = Swatch.PrimaryDarkest;

        public static Color Text = Swatch.DbLight;
        public static Color DimText = Swatch.SecondaryDarkest;
        public static Color Background = Color.Black;

        public static Color RowHighlight = Swatch.ComplimentDarkest;
        public static Color ButtonBackground = Swatch.Alternate;
        public static Color ButtonBorder = Swatch.Secondary;
        public static Color ButtonHover = Swatch.AlternateDarker;

        public static Color BorderColor = Swatch.Primary;
        public static Color HighlightColor = Swatch.Secondary;

        // Elements
        public static Color Lightning = Color.FromArgb(0, 255, 0);
        public static Color Fire = Color.FromArgb(249, 75, 10);
        public static Color Earth = Color.FromArgb(255, 255, 0);
        public static Color Water = Color.FromArgb(0, 160, 255);
        public static Color Neutral = Swatch.DbLight;

        // Map features
        public static Color Door = Swatch.DbBrightWood;
        public static Color Exit = Swatch.DbLight;
        public static Color FireAccent = Color.FromArgb(240, 188, 25);
        public static Color WaterAccent = Color.FromArgb(11, 102, 189);

        public static Color Blend(this in Color c1, in Color c2, double alpha)
        {
            return Color.FromArgb(
                (int)(c1.R + (c2.R - c1.R) * alpha),
                (int)(c1.G + (c2.G - c1.G) * alpha),
                (int)(c1.B + (c2.B - c1.B) * alpha));
        }

        public static Color FromHSL(double h, double s, double l)
        {
            // adapted from https://jsfiddle.net/Lamik/reuk63ay/91
            int f(int n)
            {
                double a = s * Math.Min(l, 1 - l);
                double k = (n + h / 30) % 12;
                return (int)((l - a * Math.Max(Math.Min(Math.Min(k - 3, 9 - k), 1), -1)) * 256);
            }
            return Color.FromArgb(f(0), f(8), f(4));
        }

        public static Color RandomColor()
        {
            int h = BaseGame.VisRand.Next(360);
            double s = BaseGame.VisRand.NextDouble() * 0.4 + 0.5;
            double l = BaseGame.VisRand.NextDouble() * 0.25 + 0.4;
            return FromHSL(h, s, l);
        }
    }
}
