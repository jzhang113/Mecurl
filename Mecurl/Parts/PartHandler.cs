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
        public ICollection<Part> PartList { get; }
        public Rectangle Bounds { get; private set; }
        public Loc Facing { get; private set; }

        public PartHandler(Loc facing)
        {
            Facing = facing;
            PartList = new List<Part>();
            Bounds = new Rectangle(0, 0, 0, 0);
        }

        public PartHandler(Loc facing, IEnumerable<Part> parts) : this(facing)
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
            return true;
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

        internal void AssignDamage(IEnumerable<Loc> targets, double power)
        {
            foreach (Part p in PartList)
            {
                p.Health -= power;
            }
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
