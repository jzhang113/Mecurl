﻿using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Mecurl.Engine;
using Mecurl.Parts;
using Optional;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Mecurl.Actors
{
    public class Mech : BaseActor
    {
        public PartHandler PartHandler { get; set; }

        public double CurrentHeat { get; private set; }
        public int Awareness { get; }
        
        public Direction Facing => PartHandler.Facing;

        protected IMessageHandler _messages;

        public Mech(in Loc pos, int hp, char symbol, Color color) : base(pos, hp, symbol, color)
        {
            CurrentHeat = 0;
            Awareness = 30;

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

            // use coolant when heat is too high
            if (CurrentHeat > PartHandler.TotalHeatCapacity && PartHandler.Core.Coolant > 0)
            {
                UseCoolant();
                return Option.Some<ICommand>(new WaitCommand(this, EngineConsts.COOL_USE_TICKS));
            }

            // dumb mech ai - order of priorities
            // 1. shooting the enemy
            // 2. chasing the enemy (weapons off cooldown)
            // 3. running away (weapons on cooldown)
            // 4. wander

            var map = Game.MapHandler;
            var groupn = new int[] { 1, 2, 3, 4, 5, 6 };
            (int firstn, _) = groupn
                .Select(n => (n, PartHandler.WeaponGroup.CanFireGroup(n - 1)))
                .FirstOrDefault(status => status.Item2);

            if (firstn > 0)
            {
                // attack if possible
                Option<ICommand> attack = PartHandler.WeaponGroup.FireGroup(this, firstn - 1);
                if (attack.HasValue) return attack;
                     
                // we have weapons, look for enemies
                if (map.PlayerMap[Pos.X, Pos.Y] < Awareness)
                {
                    Direction dir = Distance.GetNearestDirection(Game.Player.Pos, Pos);
                    if (dir == Facing || dir == Facing.Left() || dir == Facing.Right())
                    {
                        LocCost move = map.MoveTowardsTarget(Pos, map.PlayerMap, Measure.Euclidean);
                        return Option.Some<ICommand>(new MoveCommand(this, move.Loc));
                    }
                    else
                    {
                        return Option.Some<ICommand>(new TurnCommand(this, -1));
                    }
                }
                else
                {
                    // no nearby enemies, wander
                    ICommand command = map.GetPointsInRadius(Pos, 1, Measure.Euclidean)
                        .Random(Game.Rand)
                        .Match<ICommand>(
                            some: pos => new MoveCommand(this, pos),
                            none: () => new WaitCommand(this));

                    return Option.Some(command);
                }
            }
            else
            {
                // we don't have weapons, are we in danger?
                if (map.PlayerMap[Pos.X, Pos.Y] < Awareness)
                {
                    int maxDist = 0;
                    Loc maxLoc = Pos;
                    foreach (Loc loc in map.GetPointsInRadius(Pos, 1, Measure.Euclidean))
                    {
                        var dist = Distance.EuclideanSquared(loc, Game.Player.Pos);
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                            maxLoc = loc;
                        }
                    }

                    return Option.Some<ICommand>(new MoveCommand(this, maxLoc));
                }
                else
                {
                    ICommand command = map.GetPointsInRadius(Pos, 1, Measure.Euclidean)
                        .Random(Game.Rand)
                        .Match<ICommand>(
                            some: pos => new MoveCommand(this, pos),
                            none: () => new WaitCommand(this));

                    return Option.Some(command);
                }
            }
        }

        public override bool DeathCheck() => PartHandler.Core.Stability <= 0;

        internal void RotateLeft()
        {
            Game.MapHandler.RemoveFromMechTileMap(this);
            PartHandler.RotateLeft();
            Game.MapHandler.AddToMechTileMap(this, Pos);
        }

        internal void RotateRight()
        {
            Game.MapHandler.RemoveFromMechTileMap(this);
            PartHandler.RotateRight();
            Game.MapHandler.AddToMechTileMap(this, Pos);
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
                Game.MapHandler.RemoveFromMechTileMap(this);
                PartHandler.Remove(p);
                Game.MapHandler.AddToMechTileMap(this, Pos);

                // draw some debris
                for (int x = 0; x < p.Bounds.Width; x++)
                {
                    for (int y = 0; y < p.Bounds.Height; y++)
                    {
                        int xPos = Pos.X + x + p.Bounds.Left;
                        int yPos = Pos.Y + y + p.Bounds.Top;
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

        internal void UseCoolant()
        {
            double coolant = PartHandler.Core.Coolant;
            if (coolant <= 0)
            {
                _messages.Add("[color=warn]Alert[/color]: No coolant remaining");
            }
            else
            {
                _messages.Add("[color=info]Info[/color]: Flushing coolant");
                double coolantUsed = Math.Min(coolant, EngineConsts.COOL_USE_AMT);
                CurrentHeat = Math.Max(CurrentHeat - coolantUsed * EngineConsts.COOL_POWER, 0);
                PartHandler.Core.Coolant -= coolantUsed;
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

            // heat damage
            if (CurrentHeat > PartHandler.TotalHeatCapacity)
            {
                PartHandler.PartList.Random(Game.Rand).MatchSome(part =>
                {
                    // some damage computation here
                    double damage = EngineConsts.HEAT_DAMAGE;
                    part.Stability -= damage;
                    _messages.Add($"[color=warn]Alert[/color]: {part.Name} took {damage} damage from overheating");

                    if (part.Stability <= 0)
                    {
                        Game.MapHandler.RemoveFromMechTileMap(this);
                        PartHandler.Remove(part);
                        Game.MapHandler.AddToMechTileMap(this, Pos);

                        _messages.Add($"[color=err]Warning[/color]: {part.Name} destroyed by heat");
                    }
                });
            }
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

            PartHandler.Draw(layer, Pos, Color);
        }
    }
}
