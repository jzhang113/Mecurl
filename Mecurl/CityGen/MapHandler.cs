using BearLib;
using Engine;
using Engine.Drawing;
using Engine.Map;
using Mecurl.Actors;
using Mecurl.Parts;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.CityGen
{
    public class MapHandler : BaseMapHandler
    {
        // HACK: this should really be a quadtree or something
        // Tiles of each mech on  the map
        internal (char, Color, int)[,] MechTileMap { get; }

        public MapHandler(int width, int height, int level) : base(width, height, level)
        {
            MechTileMap = new (char, Color, int)[width, height];
        }

        internal void AddMech(Mech mech, in Loc pos)
        {
            ForceSetMechPosition(mech, pos);
            Game.EventScheduler.AddActor(mech);
        }

        // place a mech and destroy any walls in the way
        internal bool ForceSetMechPosition(Mech mech, in Loc pos)
        {
            Tile tile = Field[mech.Pos];
            tile.IsOccupied = false;
            tile.BlocksLight = false;
            if (Units.Remove(ToIndex(mech.Pos)))
            {
                RemoveFromMechTileMap(mech);
            }

            var bounds = mech.PartHandler.Bounds;
            int xPos = Math.Clamp(pos.X, -bounds.Left, Width - bounds.Right);
            int yPos = Math.Clamp(pos.Y, -bounds.Top, Height - bounds.Bottom);
            var clampPos = new Loc(xPos, yPos);

            mech.Pos = clampPos;
            Tile newTile = Field[clampPos];
            newTile.IsOccupied = true;
            newTile.BlocksLight = mech.BlocksLight;
            Units.Add(ToIndex(clampPos), mech);
            AddToMechTileMap(mech, clampPos);

            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                for (int y = bounds.Top; y < bounds.Bottom; y++)
                {
                    Field[x + xPos, y + yPos].IsWall = false;
                }
            }

            return true;
        }

        internal void PlaceActors(MissionInfo info, Random rand)
        {
            for (int i = 0; i < info.Enemies; i++)
            {
                double chance = rand.NextDouble();
                PartHandler ph;
                if (chance < 0.5)
                {
                    ph = BuildMissileMech();
                }
                else if (chance < 0.8)
                {
                    ph = BuildLaserMech();
                }
                else
                {
                    ph = BuildSniperMech();
                }

                var m = new Mech(new Loc(1, 1), 'x', Color.Red, this, ph);
                Loc pos = GetRandomOpenPoint();
                AddMech(m, pos);
            }

            // find a sufficiently large place to place the mech
            var player = new Player(new Loc(1, 1), this, Game.Blueprint);
            Rectangle playerBounds = player.PartHandler.Bounds;
            int minClearance = Math.Max(playerBounds.Width, playerBounds.Height);

            GetRandomOpenPoint(minClearance, 50).Match(
                some: pos =>
                {
                    // adjust the player position so that the top left corner of playerBounds is on
                    // the returned position
                    Loc centerPos = new Loc(pos.X - playerBounds.Left, pos.Y - playerBounds.Top);
                    AddMech(player, centerPos);
                },
                none: () =>
                {
                    // if we can't find a place to drop the player, just pick a random spot and
                    // destroy any offending building tiles
                    Loc pos = GetRandomOpenPoint();
                    AddMech(player, pos);
                });

            Game.Player = player;
        }

        private PartHandler BuildMissileMech()
        {
            var core = PartFactory.BuildSmallCore();
            var w1 = PartFactory.BuildSmallMissile(true);
            var w2 = PartFactory.BuildSmallMissile(false);
            var l1 = PartFactory.BuildLeg();
            l1.Center = new Loc(-2, 0);
            var l2 = PartFactory.BuildLeg();
            l2.Center = new Loc(2, 0);

            var ph = new PartHandler(core, new List<Part>()
            {
                l1, l2,
                w1, w2
            });

            ph.WeaponGroup.Add(w1, 0);
            ph.WeaponGroup.Add(w2, 0);
            return ph;
        }

        private PartHandler BuildLaserMech()
        {
            var core = PartFactory.BuildSmallCore();
            var w1 = PartFactory.BuildSmallLaser();
            w1.Center = new Loc(-3, 0);
            var w2 = PartFactory.BuildSmallLaser();
            w2.Center = new Loc(3, 0);
            var l1 = PartFactory.BuildLeg();
            l1.Center = new Loc(-2, 0);
            var l2 = PartFactory.BuildLeg();
            l2.Center = new Loc(2, 0);

            var ph = new PartHandler(core, new List<Part>()
            {
                l1, l2,
                w1, w2
            });

            ph.WeaponGroup.Add(w1, 0);
            ph.WeaponGroup.Add(w2, 0);
            return ph;
        }

        private PartHandler BuildSniperMech()
        {
            var core = PartFactory.BuildSmallCore();
            var w1 = PartFactory.BuildSniper();
            w1.Center = new Loc(2, -3);
            var l1 = PartFactory.BuildLeg();
            l1.Center = new Loc(-2, 1);
            var l2 = PartFactory.BuildLeg();
            l2.Center = new Loc(2, 1);

            var ph = new PartHandler(core, new List<Part>()
            {
                l1, l2,
                w1
            });

            ph.WeaponGroup.Add(w1, 0);
            return ph;
        }

        public override void Draw(LayerInfo layer)
        {
            base.Draw(layer);

            for (int dx = 0; dx < layer.Width; dx++)
            {
                for (int dy = 0; dy < layer.Height; dy++)
                {
                    int newX = Camera.X + dx;
                    int newY = Camera.Y + dy;

                    if (newX >= Width || newY >= Height)
                        continue;

                    Tile tile = Field[newX, newY];
                    if (!tile.IsExplored)
                        continue;

                    if (tile.IsVisible)
                    {
                        (char mechTile, Color color, _) = MechTileMap[newX, newY];
                        Terminal.Color(color);
                        Terminal.Layer(2);
                        layer.Put(dx, dy, mechTile);
                        Terminal.Layer(1);
                    }
                }
            }
        }

        // managing the MechTileMap
        internal void RemoveFromMechTileMap(Mech mech)
        {
            foreach (var part in mech.PartHandler.PartList)
            {
                for (int x = 0; x < part.Bounds.Width; x++)
                {
                    for (int y = 0; y < part.Bounds.Height; y++)
                    {
                        // we still need to check if a part is not empty in the mech
                        // otherwise, we might accidentally delete part of something else
                        int boundsIndex = part.BoundingIndex(x, y);
                        if (part.IsPassable(boundsIndex)) continue;

                        MechTileMap[x + part.Bounds.Left + mech.Pos.X, y + part.Bounds.Top + mech.Pos.Y] = (' ', Colors.Background, 0);
                    }
                }
            }
        }

        internal void AddToMechTileMap(Mech mech, Loc pos)
        {
            foreach (var part in mech.PartHandler.PartList)
            {
                for (int x = 0; x < part.Bounds.Width; x++)
                {
                    for (int y = 0; y < part.Bounds.Height; y++)
                    {
                        int boundsIndex = part.BoundingIndex(x, y);
                        if (part.IsPassable(boundsIndex)) continue;

                        MechTileMap[x + part.Bounds.Left + pos.X, y + part.Bounds.Top + pos.Y] = (part.GetPiece(boundsIndex), mech.Color, mech.Id);
                    }
                }
            }
        }
    }
}
