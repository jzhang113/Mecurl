using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Parts.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Mecurl.Parts
{
    public class PartHandler : IEnumerable<Part>
    {
        // TODO: should probably be passed in
        public Part Core { get; internal set; }

        public IList<Part> PartList { get; }
        public WeaponGroup WeaponGroup { get; }

        public Rectangle Bounds { get; private set; }
        public Direction Facing { get; private set; }

        public double TotalHeatCapacity { get; private set; }
        public double Coolant { get; internal set; }

        public PartHandler()
        {
            Facing = Direction.N;
            PartList = new List<Part>();
            WeaponGroup = new WeaponGroup();

            Bounds = new Rectangle(0, 0, 0, 0);
            TotalHeatCapacity = 0;
        }

        public PartHandler(IEnumerable<Part> parts) : this()
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
            p.Get<HeatComponent>().MatchSome(hc =>
            {
                TotalHeatCapacity += hc.HeatCapacity;
                Coolant += hc.MaxCoolant;
            });

            return true;
        }

        public void Remove(Part p)
        {
            PartList.Remove(p);

            if (p.Has<ActivateComponent>())
            {
                WeaponGroup.Remove(p);
            }

            // HACK: I don't know a way to fix the bounding box without recalculating it
            Bounds = CalculateBounds(0, 0, 0, 0);

            // update heat capacity
            p.Get<HeatComponent>().MatchSome(comp => TotalHeatCapacity -= comp.HeatCapacity);
        }

        private Rectangle CalculateBounds(int left, int top, int right, int bot)
        {
            foreach (Part remaining in PartList)
            {
                left = Math.Min(remaining.Bounds.Left, left);
                top = Math.Min(remaining.Bounds.Top, top);
                right = Math.Max(remaining.Bounds.Right, right);
                bot = Math.Max(remaining.Bounds.Bottom, bot);
            }

            return Rectangle.FromLTRB(left, top, right, bot);
        }

        public int GetMoveSpeed()
        {
            double speedMult = Core.Get<CoreComponent>().Match(
                some: comp => comp.SpeedMultiplier,
                none: () => 1);

            // TODO: revisit this for balancing
            // multiply the base time cost by core type
            double timeCost = EngineConsts.TURN_TICKS * speedMult;

            // every part has an associated speed delta
            // this is generally positive on heavy parts and negative on propulsion
            foreach (Part p in PartList)
            {
                p.Get<SpeedComponent>().MatchSome(comp =>
                {
                    timeCost += comp.SpeedDelta;
                });
            }

            // limit move speed changes up to 4x 
            return Math.Clamp((int)timeCost, 30, 480);
        }

        public double GetMaxCoolant()
        {
            double total = 0;
            foreach (Part p in PartList)
            {
                p.Get<HeatComponent>().MatchSome(comp => total += comp.MaxCoolant);
            }

            return total;
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

        internal void Validate()
        {
            // anything intersecting the core is invalid
            foreach (Part p in PartList)
            {
                p.Invalid = p.Get<CoreComponent>().Match(
                    some: _ => false,
                    none: () => p.Intersects(Core));
            }

            // some flood fill thing to check for connectivity
            // no clue if this is even correct
            var queue = new Queue<Loc>();
            var seen = new List<Loc>();
            ExpandRectPoints(Core, queue, seen);

            // list of parts we haven't processed
            var remaining = new List<Part>();
            foreach (Part p in PartList)
            {
                if (p.Invalid) continue;
                if (p.Get<CoreComponent>().HasValue) continue;

                remaining.Add(p);
            }

            while (queue.Count > 0)
            {
                Loc loc = queue.Dequeue();

                foreach (Loc next in Neighbors(loc))
                {
                    if (seen.Contains(next)) continue;

                    var borderlist = new List<Part>();
                    foreach (Part p in remaining)
                    {
                        if (p.Bounds.Contains(next.X, next.Y))
                            borderlist.Add(p);
                    }

                    if (borderlist.Count >= 1)
                    {
                        int randIndex = Game.Rand.Next(borderlist.Count);

                        for (int i = 0; i < borderlist.Count; i++)
                        {
                            if (i != randIndex)
                            {
                                borderlist[i].Invalid = true;
                                remaining.Remove(borderlist[i]);
                            }
                            else
                            {
                                ExpandRectPoints(borderlist[i], queue, seen);
                            }

                            remaining.Remove(borderlist[i]);
                        }
                    }
                }
            }

            foreach (Part p in remaining)
            {
                p.Invalid = true;
            }

            // brute force collision check
            for (int i = 0; i < PartList.Count - 1; i++)
            {
                Part p1 = PartList[i];
                if (p1.Invalid) continue;

                for (int j = i + 1; j < PartList.Count; j++)
                {
                    Part p2 = PartList[j];
                    if (p2.Invalid) continue;

                    if (p1.Intersects(p2))
                    {
                        p1.Invalid = true;
                        p2.Invalid = true;
                    }
                }
            }
        }

        internal void ValidateAndFix()
        {
            Validate();
            foreach (Part part in PartList.Where(p => p.Invalid).ToList())
            {
                Remove(part);
            }

            Bounds = CalculateBounds(0, 0, 0, 0);
        }

        private void ExpandRectPoints(Part part, Queue<Loc> queue, List<Loc> seen)
        {
            for (int x = 0; x < part.Bounds.Width; x++)
            {
                for (int y = 0; y < part.Bounds.Height; y++)
                {
                    int boundsIndex = part.BoundingIndex(x, y);
                    if (part.IsPassable(boundsIndex)) continue;

                    int newX = x + part.Bounds.Left;
                    int newY = y + part.Bounds.Top;
                    var loc = new Loc(newX, newY);

                    queue.Enqueue(loc);
                    seen.Add(loc);
                }
            }
        }

        private static IEnumerable<Loc> Neighbors(Loc pos)
        {
            yield return pos + Direction.N;
            yield return pos + Direction.E;
            yield return pos + Direction.S;
            yield return pos + Direction.W;
        }

        internal void Draw(LayerInfo layer, Loc pos, Color color)
        {
            Terminal.Layer(2);
            foreach (Part p in PartList)
            {
                Terminal.Color(p.Invalid ? Color.Red : color);
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
