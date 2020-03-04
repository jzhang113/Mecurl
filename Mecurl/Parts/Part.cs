using Engine;
using Engine.Drawing;
using Optional;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.Parts
{
    public class Part
    {
        internal static int GlobalId = 0;
        internal int Id { get; }

        public string Name { get; set; }
        public double MaxHealth { get; set; }
        public double Health { get; set; }

        public RotateChar[] Structure { get; }
        public int Width { get; }
        public int Height { get; }

        public Loc Facing { get; private set; }
        public Loc Center { get; private set; }
        public Rectangle Bounds { get; private set; }

        public Func<Option<ICommand>> Activate { get; }

        public Part(int width, int height, Loc center, Loc facing, RotateChar[] structure, Func<Option<ICommand>> activate)
        {
            Id = GlobalId++;
            Structure = structure;
            Activate = activate;

            Width = width;
            Height = height;
            Center = center;
            Facing = facing;
            UpdateBounds();

            MaxHealth = 100;
            Health = MaxHealth;
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

        internal void RotateLeft()
        {
            Facing = Facing.Left().Left();
            Center = new Loc(Center.Y, -Center.X);
            Bounds = Rectangle.FromLTRB(Bounds.Top, -Bounds.Right + 1, Bounds.Bottom, -Bounds.Left + 1);
         
            for (int i = 0; i < Structure.Length; i++)
            {
                Structure[i] = Structure[i].Left;
            }
        }

        internal void RotateRight()
        {
            Facing = Facing.Right().Right();
            Center = new Loc(-Center.Y, Center.X);
            Bounds = Rectangle.FromLTRB(-Bounds.Bottom + 1, Bounds.Left, -Bounds.Top + 1, Bounds.Right);

            for (int i = 0; i < Structure.Length; i++)
            {
                Structure[i] = Structure[i].Right;
            }
        }

        internal bool IsPassable(int x)
        {
            char c = GetPiece(x);
            return c == ' ';
        }

        internal bool IsMergable(int x)
        {
            char c = GetPiece(x);
            return c == '/' || c == '\\';
        }

        internal char GetPiece(int x)
        {
            int y = Adjust(x);
            return Structure[y].Char;
        }

        internal int Adjust(int idx)
        {
            if (Facing == Direction.N)
            {
                return idx;
            }
            else if (Facing == Direction.S)
            {
                return Structure.Length - idx - 1;
            }
            else if (Facing == Direction.W)
            {
                return Width + idx / Width * Width - idx % Width - 1;
            }
            else if (Facing == Direction.E)
            {
                return Structure.Length - Width - idx / Width * Width + idx % Width;
            }
            else
            {
                return -1;
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
            for (int x = Bounds.Left; x < Bounds.Right; x++)
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
                    char piece0 = Structure[x0 + y0 * Bounds.Width].Char;

                    int x1 = x - other.Bounds.Left;
                    int y1 = y - other.Bounds.Top;
                    char piece1 = other.Structure[x1 + y1 * other.Bounds.Width].Char;

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

            if (Facing == Direction.N || Facing == Direction.S)
            {
                for (int i = 0; i < Structure.Length; i++)
                {
                    if (IsPassable(i))
                    {
                        continue;
                    }
                    layer.Put(
                        Bounds.Left + i % Width + pos.X - Camera.X,
                        Bounds.Top + i / Width + pos.Y - Camera.Y,
                        GetPiece(i));
                }
            }
            else if (Facing == Direction.W || Facing == Direction.E)
            {
                for (int i = 0; i < Structure.Length; i++)
                {
                    if (IsPassable(i))
                    {
                        continue;
                    }
                    layer.Put(
                        Bounds.Left + i / Width + pos.X - Camera.X,
                        Bounds.Top + i % Width + pos.Y - Camera.Y,
                        GetPiece(i));
                }
            }
        }
    }
}
