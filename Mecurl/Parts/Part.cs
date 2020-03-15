using Engine;
using Engine.Drawing;
using Mecurl.Parts.Components;
using Optional;
using RexTools;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.Parts
{
    public class Part
    {
        internal static int GlobalId = 0;

        internal int Id { get; }

        public string Name { get; set; }
        public TileMap Art { get; set; }

        public RotateChar[] Structure { get; }
        public int Width { get; }
        public int Height { get; }

        public Rectangle Bounds { get; private set; }
        public Direction Facing { get; private set; }
        public Loc Center
        {
            get => _center;
            internal set
            {
                _center = value;
                UpdateBounds();
            }
        }

        protected IList<IPartComponent> Components { get; }

        private Loc _center;
        internal bool Invalid { get; set; }

        public Part(string name, int width, int height, RotateChar[] structure, Direction facing = Direction.N)
        {
            Id = GlobalId++;
            Structure = structure;

            Name = name;
            Width = width;
            Height = height;

            _center = new Loc(0, 0);
            Facing = facing;
            UpdateBounds();

            Invalid = false;

            Components = new List<IPartComponent>();
        }

        #region Managing components
        public bool Add<T>(T component) where T : IPartComponent
        {
            if (Has<T>()) return false;

            Components.Add(component);
            return true;
        }

        public bool Has<T>()
        {
            foreach (IPartComponent comp in Components)
            {
                if (comp.GetType() == typeof(T))
                {
                    return true;
                }
            }

            return false;
        }

        public Option<T> Get<T>()
        {
            foreach (IPartComponent comp in Components)
            {
                if (comp.GetType() == typeof(T))
                {
                    return Option.Some((T)comp);
                }
            }

            return Option.None<T>();
        }
        #endregion

        #region Facing
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
            _center = new Loc(Center.Y, -Center.X);
            Bounds = Rectangle.FromLTRB(Bounds.Top, -Bounds.Right + 1, Bounds.Bottom, -Bounds.Left + 1);

            for (int i = 0; i < Structure.Length; i++)
            {
                Structure[i] = Structure[i].Left;
            }
        }

        internal void RotateRight()
        {
            Facing = Facing.Right().Right();
            _center = new Loc(-Center.Y, Center.X);
            Bounds = Rectangle.FromLTRB(-Bounds.Bottom + 1, Bounds.Left, -Bounds.Top + 1, Bounds.Right);

            for (int i = 0; i < Structure.Length; i++)
            {
                Structure[i] = Structure[i].Right;
            }
        }
        #endregion

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

        // this is another indexing of Structure
        // the problem here is that we always iterate over the array from left to right
        // however, when we rotate the array, this left to right reading does not correspond
        // with the indices that the rotated x and y correspond to
        // thus, we need to apply an adjustment depending on the Facing
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

        // this is actually a third indexing of Structure, and it depends on the current facing
        // note that if the facing is N/S, then p.Bounds.Width == p.Width, so the boundsIndex
        // correspond to the indices of Structure before adjustment
        // however, if the facing is W/E, then the dimensions are flipped, so we need to compute
        // index with x and y flipped and then adjust
        internal int BoundingIndex(int x, int y)
        {
            if (Facing == Direction.N || Facing == Direction.S)
            {
                return x + y * Width;
            }
            else if (Facing == Direction.W || Facing == Direction.E)
            {
                return x * Width + y;
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

                    int boundsIndex = BoundingIndex(x - Bounds.Left, y - Bounds.Top);
                    if (IsPassable(boundsIndex)) continue;


                    char piece0 = GetPiece(boundsIndex);


                    int x1 = x - other.Bounds.Left;
                    int y1 = y - other.Bounds.Top;
                    char piece1 = other.GetPiece(other.BoundingIndex(x1, y1));

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
