using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Mecurl.Engine;
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

        public double CurrentHeat { get; private set; }
        public Direction Facing => PartHandler.Facing;

        protected IMessageHandler _messages;

        public Mech(in Loc pos, int hp, char symbol, Color color) : base(pos, hp, symbol, color)
        {
            CurrentHeat = 0;

            _messages = new DummyMessageHandler();
        }

        public override Option<ICommand> TriggerDeath()
        {
            Game.MapHandler.RemoveActor(this);

            if (Game.MapHandler.Field[Pos].IsVisible)
            {
                Game.MessagePanel.Add($"[color=info]Info[/color]: {Name} destroyed");
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
            var removeList = new List<Part>();

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
                        // some damage computation here
                        double damage = power;
                        p.Stability -= damage;
                        _messages.Add($"[color=warn]Alert[/color]: {p.Name} took {damage} damage");

                        if (p.Stability <= 0)
                        {
                            removeList.Add(p);
                            _messages.Add($"[color=err]Warning[/color]: {p.Name} destroyed");
                        }
                        break;
                    }
                }
            }

            foreach (Part p in removeList)
            {
                // remove this part from any associated structures
                PartHandler.Remove(p);

                // draw some debris
                for (int x = 0; x < p.Bounds.Width; x++)
                {
                    for (int y = 0; y < p.Bounds.Height; y++)
                    {
                        int xPos = Pos.X + x + p.Center.X;
                        int yPos = Pos.Y + y + p.Center.Y;
                        var tile = Game.MapHandler.Field[xPos, yPos];

                        if (!tile.IsWall)
                        {
                            tile.Color = Color.Gray;
                            tile.Symbol = CharUtils.GetRubbleSymbol();
                        }
                    }
                }

            }
        }

        internal void ProcessTick()
        {
            foreach (Part p in PartHandler)
            {
                UpdateHeat(-p.HeatRemoved);

                if (p.CurrentCooldown > 0)
                {
                    p.CurrentCooldown--;

                    if (p.CurrentCooldown == 0)
                    {
                        _messages.Add($"[color=info]Info[/color]: {p.Name} ready");
                    }
                }
            }

            CurrentHeat = Math.Max(CurrentHeat, 0);
        }

        internal void UpdateHeat(double delta)
        {
            if (delta >= 0)
            {
                // only warn once when passing thresholds
                double prevHeat = CurrentHeat;
                double newHeat = CurrentHeat + delta;

                double criticalThresh = PartHandler.TotalHeatCapacity;
                double warnThresh = PartHandler.TotalHeatCapacity * 2 / 3;

                if (prevHeat < criticalThresh && newHeat >= criticalThresh)
                {
                    _messages.Add("[color=err]Warning[/color]: Heat level critical");
                }
                else if (prevHeat < warnThresh && newHeat >= warnThresh)
                {
                    _messages.Add("[color=warn]Alert[/color]: Heat level high");
                }

                CurrentHeat = newHeat;
            }
            else
            {
                CurrentHeat += delta;
            }
        }

        public override void Draw(LayerInfo layer)
        {
            if (!ShouldDraw) return;

            Terminal.Color(Color);
            PartHandler.Draw(layer, Pos);
        }
    }
}
