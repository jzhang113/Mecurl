using BearLib;
using Engine;
using Engine.Drawing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.Parts
{
    public class PartHandler : IEnumerable<Part>
    {
        // TODO: should probably be passed in
        public Part Core { get; internal set; }

        public ICollection<Part> PartList { get; }
        public Rectangle Bounds { get; private set; }
        public Direction Facing { get; private set; }
        public double TotalHeatCapacity { get; private set; }

        public PartHandler(Direction facing)
        {
            Facing = facing;
            PartList = new List<Part>();
            Bounds = new Rectangle(0, 0, 0, 0);

            TotalHeatCapacity = 0;
        }

        public PartHandler(Direction facing, IEnumerable<Part> parts) : this(facing)
        {
            foreach (Part p in parts)
            {
                Add(p);
            }
        }

        public bool Add(Part p)
        {
            // check space available
            foreach (Part existing in PartList)
            {
                if (p.Intersects(existing))
                {
                    return false;
                }
            }
            PartList.Add(p);

            // update bounding box
            int newLeft = Math.Min(p.Bounds.Left, Bounds.Left);
            int newTop = Math.Min(p.Bounds.Top, Bounds.Top);
            int newRight = Math.Max(p.Bounds.Right, Bounds.Right);
            int newBot = Math.Max(p.Bounds.Bottom, Bounds.Bottom);

            Bounds = Rectangle.FromLTRB(newLeft, newTop, newRight, newBot);

            // add heat capacity to total
            TotalHeatCapacity += p.HeatCapacity;
            return true;
        }

        public void Remove(Part p)
        {
            PartList.Remove(p);

            // HACK: I don't know a way to fix the bounding box without recalculating it
            int left = 0;
            int top = 0;
            int right = 0;
            int bot = 0;

            foreach (Part remaining in PartList)
            {
                left = Math.Min(remaining.Bounds.Left, left);
                top = Math.Min(remaining.Bounds.Top, top);
                right = Math.Max(remaining.Bounds.Right, right);
                bot = Math.Max(remaining.Bounds.Bottom, bot);
            }

            Bounds = Rectangle.FromLTRB(left, top, right, bot);

            // update heat capacity
            TotalHeatCapacity -= p.HeatCapacity;
        }

        internal void RotateRight()
        {
            Facing = Facing.Right().Right();
            foreach (Part p in PartList)
            {
                p.RotateRight();
            }

            // Fix bounding box too
            Bounds = Rectangle.FromLTRB(-Bounds.Bottom + 1, Bounds.Left, -Bounds.Top + 1, Bounds.Right);
        }

        internal void RotateLeft()
        {
            Facing = Facing.Left().Left();
            foreach (Part p in PartList)
            {
                p.RotateLeft();
            }

            // Fix bounding box too
            Bounds = Rectangle.FromLTRB(Bounds.Top, -Bounds.Right + 1, Bounds.Bottom, -Bounds.Left + 1);
        }

        internal void Draw(LayerInfo layer, Loc pos)
        {
            Terminal.Layer(2);
            foreach (Part p in PartList)
            {
                p.Draw(layer, pos);
            }

            Terminal.Layer(1);
        }

        #region IEnumerable overrides
        public IEnumerator<Part> GetEnumerator()
        {
            return PartList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
