using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Mecurl.Parts;
using Optional;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.Actors
{
    public class Actor : BaseActor
    {
        public PartHandler PartHandler { get; protected set; }
        public WeaponGroup WeaponGroup { get; protected set; }

        public Direction Facing => PartHandler.Facing;

        public Actor(in Loc pos, int hp, char symbol, Color color) : base(pos, hp, symbol, color)
        {
        }

        public override Option<ICommand> TriggerDeath()
        {
            Game.MapHandler.RemoveActor(this);

            if (Game.MapHandler.Field[Pos].IsVisible)
            {
                Game.MessagePanel.AddMessage($"{Name} dies");
                Game.MapHandler.Refresh();
            }

            return Option.None<ICommand>();
        }

        public override Option<ICommand> GetAction()
        {
            return Option.Some<ICommand>(new WaitCommand(this));
        }

        internal void RotateLeft()
        {
            PartHandler.RotateLeft();
        }

        internal void RotateRight()
        {
            PartHandler.RotateRight();
        }

        internal void AssignDamage(ICollection<Loc> targets, double power)
        {
            foreach (Part p in PartHandler.PartList)
            {
                for (int i = 0; i < p.Structure.Length; i++)
                {
                    if (p.IsPassable(i))
                    {
                        continue;
                    }

                    int dx, dy;
                    if (p.Facing == Direction.N || p.Facing == Direction.S)
                    {
                        dx = i % p.Width;
                        dy = i / p.Width;
                    }
                    else
                    {
                        dx = i / p.Width;
                        dy = i % p.Width;
                    }

                    Loc currPos = Pos + (p.Bounds.Left + dx, p.Bounds.Top + dy);
                    if (targets.Contains(currPos))
                    {
                        p.Health -= power;
                        break;
                    }
                }
            }
        }

        public override void Draw(LayerInfo layer)
        {
            if (!ShouldDraw)
                return;

            Terminal.Color(Color);
            PartHandler.Draw(layer, Pos);
        }
    }
}
