﻿using Engine;
using Engine.Drawing;
using System;
using System.Drawing;

namespace Mecurl.Parts
{
    public class Part
    {
        internal static int GlobalId = 0;
        internal int Id { get; }

        public string Name { get; set; }

        public char[] Structure { get; }
        public int Width { get; }
        public int Height { get; }

        public Loc Facing { get; private set; }
        public Loc Center { get; private set; }
        public Rectangle Bounds { get; private set; }

        public Part(int width, int height, Loc center, Loc facing, char[] structure)
        {
            Id = GlobalId++;
            Structure = structure;
            Width = width;
            Height = height;
            Center = center;
            Facing = facing;
            UpdateBounds();
        }

        internal void RotateLeft()
        {
            Facing = Facing.Left().Left();
            Center = new Loc(Center.Y, -Center.X);
            Bounds = Rectangle.FromLTRB(Bounds.Top, -Bounds.Right + 1, Bounds.Bottom, -Bounds.Left + 1);
        }

        internal void RotateRight()
        {
            Facing = Facing.Right().Right();
            Center = new Loc(-Center.Y, Center.X);
            Bounds = Rectangle.FromLTRB(-Bounds.Bottom + 1, Bounds.Left, -Bounds.Top + 1, Bounds.Right);
        }

        private void UpdateBounds()
        {
            if (Facing == Direction.N || Facing == Direction.S)
            {
                Bounds = new Rectangle(Center.X - Width / 2, Center.Y - Height / 2, Width, Height);
            }
            else if (Facing == Direction.E || Facing == Direction.W)
            {
                Bounds = new Rectangle(Center.X - Height / 2, Center.Y - Width / 2, Height, Width);
            }
        }

        internal bool Intersects(Part other)
        {
            // if the bounding boxes don't intersect, the parts can't intersect
            if (!Bounds.IntersectsWith(other.Bounds))
            {
                return false;
            }

            // otherwise we have to check carefully
            for (int x = Bounds.Left ; x < Bounds.Right; x++)
            {
                for (int y = Bounds.Top; y < Bounds.Bottom; y++)
                {
                    // if this tile lies outside of the other bounding box, it can't intersect
                    if (x < other.Bounds.Left || x >= other.Bounds.Right ||
                        y < other.Bounds.Top || y >= other.Bounds.Bottom)
                    {
                        continue;
                    }

                    int x0 = x - Bounds.Left;
                    int y0 = y - Bounds.Top;
                    char piece0 = Structure[x0 + y0 * Bounds.Width];

                    int x1 = x - other.Bounds.Left;
                    int y1 = y - other.Bounds.Top;
                    char piece1 = other.Structure[x1 + y1 * other.Bounds.Width];

                    // joining logic
                    if (piece0 == ' ' || piece1 == ' ' ||
                        (piece0 == '\\' && piece1 == '\\') ||
                        (piece0 == '/' && piece1 == '/'))
                    {
                        continue;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal void Draw(LayerInfo layer, Loc pos)
        {
            if (Facing == Direction.N)
            {
                for (int i = 0; i < Structure.Length; i++)
                {
                    if (Structure[i] == ' ')
                    {
                        continue;
                    }
                    layer.Put(
                        Bounds.Left + i % Width + pos.X - Camera.X,
                        Bounds.Top + i / Width + pos.Y - Camera.Y,
                        Structure[i]);
                }
            }
            else if (Facing == Direction.S)
            {
                for (int i = 0; i < Structure.Length; i++)
                {
                    if (Structure[Structure.Length - i - 1] == ' ')
                    {
                        continue;
                    }
                    layer.Put(
                        Bounds.Left + i % Width + pos.X - Camera.X,
                        Bounds.Top + i / Width + pos.Y - Camera.Y,
                        Structure[Structure.Length - i - 1]);
                }
            }
            else if (Facing == Direction.W)
            {
                for (int i = 0; i < Structure.Length; i++)
                {
                    if (Structure[i] == ' ')
                    {
                        continue;
                    }
                    layer.Put(
                        Bounds.Left + i / Width + pos.X - Camera.X,
                        Bounds.Top + i % Width + pos.Y - Camera.Y,
                        Structure[i]);
                }
            }
            else if (Facing == Direction.E)
            {
                for (int i = 0; i < Structure.Length; i++)
                {
                    if (Structure[Structure.Length - i - 1] == ' ')
                    {
                        continue;
                    }
                    layer.Put(
                        Bounds.Left + i / Width + pos.X - Camera.X,
                        Bounds.Top + i % Width + pos.Y - Camera.Y,
                        Structure[Structure.Length - i - 1]);
                }
            }
        }
    }
}