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
    public class Mech : BaseActor
    {
        // HACK: this chain of initialization is problematic
        // we need Parts to build a PartHandler, but Weapons need a WeaponGroup, which in turn need a Mech
        public PartHandler PartHandler { get; set; }
        public WeaponGroup WeaponGroup { get; protected set; }

        public double CurrentHeat { get; internal set; }

        public Direction Facing => PartHandler.Facing;

        public Mech(in Loc pos, int hp, char symbol, Color color) : base(pos, hp, symbol, color)
        {
            WeaponGroup = new WeaponGroup(this);
            CurrentHeat = 0;
        }

        public override Option<ICommand> TriggerDeath()
        {
            Game.MapHandler.RemoveActor(this);

            if (Game.MapHandler.Field[Pos].IsVisible)
            {
                Game.MessagePanel.AddMessage($"{Name} destroyed");
                Game.MapHandler.Refresh();
            }

            return Option.None<ICommand>();
        }

        public override Option<ICommand> GetAction()
        {
            ProcessTick();
            return Option.Some<ICommand>(new WaitCommand(this));
        }

        public override bool DeathCheck() => PartHandler.Core.Stability <= 0;

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
                        p.Stability -= power;
                        break;
                    }
                }
            }
        }

        internal void ProcessTick()
        {
            foreach (Part p in PartHandler)
            {
                CurrentHeat -= p.HeatRemoved;

                if (p.CurrentCooldown > 0)
                {
                    p.CurrentCooldown--;
                }
            }

            CurrentHeat = Math.Max(CurrentHeat, 0);
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
