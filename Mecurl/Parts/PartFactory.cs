using Engine;
using Mecurl.Actors;
using Mecurl.Commands;
using Mercurl.Animations;
using Optional;
using RexTools;
using System;
using System.Collections.Generic;

using static Mecurl.Parts.RotateCharLiterals;

namespace Mecurl.Parts
{
    internal static class PartFactory
    {
        private static readonly TileMap _missileArt;

        static PartFactory()
        {
            var reader = new RexReader("AsciiArt/missileLauncher.xp");
            _missileArt = reader.GetMap();
        }

        internal static Weapon BuildSmallMissile(bool left)
        {
            var initialFacing = Direction.N;

            string name = left ? "Missiles (Left)" : "Missiles (Right)";
            RotateChar[] tiles = left ?
                new RotateChar[9] { b2, b4, sl, b2, b4, b4, sl, b2, b2 } :
                       new RotateChar[9] { sr, b4, b2, b4, b4, b2, b2, b2, sr };

            Loc pos = left ? new Loc(-2, 2) : new Loc(2, 2);

            static ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(240, new AttackCommand(m, EngineConsts.TURN_TICKS, 10, targets, anim));
            }

            return new Weapon(3, 3, pos, initialFacing, tiles, 50,
                                new TargetZone(TargetShape.Range, 20, 2), attack)
            { Name = name, Art = _missileArt, HeatGenerated = 3, Cooldown = 6, SpeedDelta = 15 };
        }

        internal static Weapon BuildLargeMissile()
        {
            RotateChar[] tiles = new RotateChar[12] { trn, trn, trn, b2, b4, b2, b3, b3, b3, sl, b2, sr };

            static ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(240, new AttackCommand(m, EngineConsts.TURN_TICKS, 10, targets, anim));
            }

            return new Weapon(3, 4, new Loc(0, 0), Direction.N, tiles, 100,
                                new TargetZone(TargetShape.Range, 30, 3, false), attack)
            { Name = "Missiles (large)", Art = _missileArt, HeatGenerated = 10, Cooldown = 15, SpeedDelta = 25 };
        }

        internal static Weapon BuildSmallLaser()
        {
            static ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(10, new AttackCommand(m, EngineConsts.TURN_TICKS, 10, targets, anim, true));
            }

             var tiles = new RotateChar[4] { trn, b4, b3, b2 };

            return new Weapon(1, 4, new Loc(0, 0), Direction.N, tiles, 50,
                                new TargetZone(TargetShape.Ray, 20, 1), attack)
            { Name = "Laser (small)", HeatGenerated = 1, Cooldown = 4, SpeedDelta = 5 };
        }

        internal static Weapon BuildLargeLaser()
        {
            static ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(60, new AttackCommand(m, EngineConsts.TURN_TICKS, 15, targets, anim, true));
            }

            var tiles = new RotateChar[8] { trn, trn, b4, b4, b3, b3, b2, b2 };

            return new Weapon(2, 4, new Loc(0, 0), Direction.N, tiles, 100,
                                new TargetZone(TargetShape.Ray, 35, 2), attack)
            { Name = "Laser (large)", HeatGenerated = 6, Cooldown = 10, SpeedDelta = 20 };
        }

        internal static Weapon BuildSniper()
        {
            static ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(10, new AttackCommand(m, EngineConsts.TURN_TICKS * 3, 25, targets, anim, true));
            }

            var tiles = new RotateChar[10] { vt, em, vt, em, vt, hz, vt, vt, sl, b3 };

            return new Weapon(2, 5, new Loc(0, 0), Direction.N, tiles, 50,
                                new TargetZone(TargetShape.Ray, 50, 1), attack)
            { Name = "Sniper", HeatGenerated = 1, Cooldown = 15, SpeedDelta = 20 };
        }

        internal static Weapon BuildFlak()
        {
            static ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(60, new AttackCommand(m, EngineConsts.TURN_TICKS * 2, 15, targets, anim));
            }

            var tiles = new RotateChar[8] { vt, vt, vt, vt, b4, b4, b4, b4 };

            return new Weapon(2, 4, new Loc(0, 0), Direction.N, tiles, 100,
                                new TargetZone(TargetShape.Range, 35, 3), attack)
            { Name = "Flak Cannon", HeatGenerated = 15, Cooldown = 15, SpeedDelta = 20 };
        }

        //internal static Weapon BuildSword()
        //{
        //    static ICommand attack(Mech m, IEnumerable<Loc> targets)
        //    {
        //        var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
        //        return new DelayAttackCommand(120, new AttackCommand(m, EngineConsts.TURN_TICKS, 20, targets, anim, true));
        //    }

        //    var tiles = new RotateChar[12] { em, dbv, em, em, dbv, em, dbh, dbc, dbh, em, vt, em };

        //    return new Weapon(3, 4, new Loc(0, 0), Direction.N, tiles, 100,
        //                        new TargetZone(TargetShape.Self, 4), attack)
        //    { Name = "Sword", HeatGenerated = 0, Cooldown = 2, SpeedDelta = -10 };
        //}

        internal static Core BuildSmallCore()
        {
            var facing = Direction.N;
            return new Core(3, 3, new Loc(0, 0), facing,
                    new RotateChar[9] { sr, arn, sl, b3, at, b3, sl, b2, sr }, 100, 1, 30)
            { Name = "Core", HeatCapacity = 30, HeatRemoved = 0.5 };
        }

        internal static Part BuildLeg(bool left)
        {
            var pos = left ? new Loc(-2, 0) : new Loc(2, 0);

            return new Part(1, 2, pos, Direction.N,
                new RotateChar[2] { arn, arn }, 30)
            { Name = "Leg", SpeedDelta = -30 };
        }


        internal static Part BuildRandom()
        {
            double chance = Game.Rand.NextDouble();
            double chance2 = Game.Rand.NextDouble();

            if (chance < 0.5)
            {
                if (chance2 < 0.5)
                    return BuildSmallLaser();
                else
                    return BuildSmallMissile(Game.Rand.NextDouble() < 0.5);
            }
            else if (chance < 0.8)
            {
                if (chance2 < 0.5)
                    return BuildLargeLaser();
                else
                    return BuildSniper();
            }
            else
            {
                if (chance2 < 0.5)
                    return BuildFlak();
                else
                    return BuildLargeMissile();
            }
        }
    }
}
