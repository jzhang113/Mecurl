using Engine;
using Mecurl.Actors;
using Mecurl.Commands;
using Mecurl.Parts.Components;
using Mercurl.Animations;
using Optional;
using RexTools;
using System.Collections.Generic;
using System.Linq;
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

        internal static Part BuildSmallMissile(bool left)
        {
            string name = left ? "Missiles (Left)" : "Missiles (Right)";
            var pos = left ? new Loc(-2, 2) : new Loc(2, 2);
            var tiles = left ?
                new RotateChar[9] { b2, b4, sl, b2, b4, b4, sl, b2, b2 } :
                new RotateChar[9] { sr, b4, b2, b4, b4, b2, b2, b2, sr };

            var target = new TargetZone(TargetShape.Range, 20, 2);
            int cooldown = 6;
            ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(EngineConsts.TURN_TICKS * 2, new AttackCommand(m, EngineConsts.TURN_TICKS, 10, targets, anim));
            }

            var p = new Part(name, 3, 3, tiles);
            p.Art = _missileArt;
            p.Center = pos;
            p.Add(new StabilityComponent(50));
            p.Add(new HeatComponent(4, 0, 0, 0));
            p.Add(new SpeedComponent(15));
            p.Add(new ActivateComponent(target, attack, cooldown));
            return p;
        }

        internal static Part BuildLargeMissile()
        {
            var tiles = new RotateChar[12] { trn, trn, trn, b2, b4, b2, b3, b3, b3, sl, b2, sr };

            var target = new TargetZone(TargetShape.Range, 30, 3, false);
            int cooldown = 15;
            ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(240, new AttackCommand(m, EngineConsts.TURN_TICKS, 10, targets, anim));
            }

            var p = new Part("Missiles (large)", 3, 4, tiles);
            p.Art = _missileArt;
            p.Add(new StabilityComponent(100));
            p.Add(new HeatComponent(10, 0, 0, 0));
            p.Add(new SpeedComponent(25));
            p.Add(new ActivateComponent(target, attack, cooldown));
            return p;
        }

        internal static Part BuildSmallLaser()
        {
            var tiles = new RotateChar[4] { trn, b4, b3, b2 };

            var target = new TargetZone(TargetShape.Ray, 20, 1);
            int cooldown = 4;
            ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new FlashAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(10, new AttackCommand(m, EngineConsts.TURN_TICKS, 10, targets, anim, true));
            }

            var p = new Part("Laser (small)", 1, 4, tiles);
            p.Add(new StabilityComponent(50));
            p.Add(new HeatComponent(2, 0, 0, 0));
            p.Add(new SpeedComponent(10));
            p.Add(new ActivateComponent(target, attack, cooldown));
            return p;
        }

        internal static Part BuildLargeLaser()
        {
            var tiles = new RotateChar[8] { trn, trn, b4, b4, b3, b3, b2, b2 };

            var target = new TargetZone(TargetShape.Ray, 35, 2);
            var cooldown = 10;
            ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(60, new AttackCommand(m, EngineConsts.TURN_TICKS, 15, targets, anim, true));
            }

            var p = new Part("Laser (large)", 2, 4, tiles);
            p.Add(new StabilityComponent(100));
            p.Add(new HeatComponent(8, 0, 0, 0));
            p.Add(new SpeedComponent(20));
            p.Add(new ActivateComponent(target, attack, cooldown));
            return p;
        }

        internal static Part BuildSniper()
        {
            var tiles = new RotateChar[10] { vt, em, vt, em, vt, hz, vt, vt, sl, b3 };

            var target = new TargetZone(TargetShape.Ray, 50, 1);
            int cooldown = 15;
            ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(10, new AttackCommand(m, EngineConsts.TURN_TICKS * 3, 25, targets, anim, true));
            }

            var p = new Part("Sniper", 2, 5, tiles);
            p.Add(new StabilityComponent(50));
            p.Add(new HeatComponent(1, 0, 0, 0));
            p.Add(new SpeedComponent(20));
            p.Add(new ActivateComponent(target, attack, cooldown));
            return p;
        }

        internal static Part BuildFlak()
        {
            var tiles = new RotateChar[8] { vt, vt, vt, vt, b4, b4, b4, b4 };

            var target = new TargetZone(TargetShape.Range, 35, 3);
            int cooldown = 15;
            ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(60, new AttackCommand(m, EngineConsts.TURN_TICKS * 2, 15, targets, anim));
            }

            var p = new Part("Flak Cannon", 2, 4, tiles);
            p.Add(new StabilityComponent(100));
            p.Add(new HeatComponent(20, 0, 0, 0));
            p.Add(new SpeedComponent(20));
            p.Add(new ActivateComponent(target, attack, cooldown));
            return p;
        }

        internal static Part BuildTeleporter()
        {
            var tiles = new RotateChar[9] { b4, b2, b4, b2, x, b2, b4, b2, b4 };

            var target = new TargetZone(TargetShape.Range, 5, 0, false, false);
            int cooldown = 25;
            ICommand teleport(Mech m, IEnumerable<Loc> targets)
            {
                var pos = targets.First();

                int newTop = m.PartHandler.Bounds.Top + pos.Y;
                int newBot = m.PartHandler.Bounds.Bottom + pos.Y;
                int newLeft = m.PartHandler.Bounds.Left + pos.X;
                int newRight = m.PartHandler.Bounds.Right + pos.X;
                bool inBounds = newTop >= 0 && newLeft >= 0 && newBot < Game.MapHandler.Height && newRight < Game.MapHandler.Width;

                if (!inBounds)
                {
                    Game.MessagePanel.Add("[color=warn]Alert[/color]: Invalid coordinates, teleport modified");
                }

                Game.MapHandler.ForceSetMechPosition(m, pos);
                return new WaitCommand(m);
            }

            var p = new Part("Portable Teleporter", 3, 3, tiles);
            p.Add(new StabilityComponent(30));
            p.Add(new HeatComponent(20, 0, 0, 0));
            p.Add(new ActivateComponent(target, teleport, cooldown));
            return p;
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

        internal static CorePart BuildSmallCore()
        {
            var tiles = new RotateChar[9] { sr, arn, sl, b3, at, b3, sl, b2, sr };

            var cc = new CoreComponent(1, 1);
            var sc = new StabilityComponent(100);
            var cp = new CorePart("Core (small)", 3, 3, tiles, cc, sc);
            cp.Add(new HeatComponent(0, 30, 0.5, 3));
            return cp;
        }

        internal static Part BuildLeg()
        {
            var tiles = new RotateChar[2] { arn, arn };

            var p = new Part("Leg", 1, 2, tiles);
            p.Add(new StabilityComponent(30));
            p.Add(new SpeedComponent(-15));
            return p;
        }

        internal static Part BuildRandom()
        {
            double chance = Game.Rand.NextDouble();
            double chance2 = Game.Rand.NextDouble();

            if (chance < 0.4)
            {
                if (chance2 < 0.5)
                    return BuildSmallLaser();
                else
                    return BuildSmallMissile(Game.Rand.NextDouble() < 0.5);
            }
            else if (chance < 0.75)
            {
                if (chance2 < 0.5)
                    return BuildLargeLaser();
                else
                    return BuildSniper();
            }
            else if (chance < 0.85)
            {
                return BuildTeleporter();
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
