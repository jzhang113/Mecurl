using Engine;
using Mecurl.Actors;
using Mecurl.Commands;
using Mercurl.Animations;
using Optional;
using RexTools;
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

            Loc pos = left ? new Loc(-2, 2) : new Loc(3, 2);

            static ICommand attack(Mech m, IEnumerable<Loc> targets)
            {
                var anim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                return new DelayAttackCommand(240, new AttackCommand(m, EngineConsts.TURN_TICKS, 10, targets, anim));
            }

            return new Weapon(3, 3, pos, initialFacing, tiles, 50,
                                new TargetZone(TargetShape.Range, 20, 2), attack)
            { Name = name, Art = _missileArt, HeatGenerated = 40, Cooldown = 100, SpeedDelta = 15 };
        }

        internal static Core BuildSmallCore()
        {
            var facing = Direction.N;
            return new Core(3, 3, new Loc(0, 0), facing,
                    new RotateChar[9] { sr, arn, sl, b3, at, b3, sl, b2, sr }, 100, 1, 30)
            { Name = "Core", HeatCapacity = 30, HeatRemoved = 0.5 };
        }
    }
}
