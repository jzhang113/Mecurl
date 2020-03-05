using BearLib;
using Engine;
using Mecurl.Commands;
using Mecurl.Parts;
using Mecurl.State;
using Mercurl.Animations;
using Optional;
using RexTools;
using System;
using System.Collections.Generic;
using System.Drawing;

using static Mecurl.Parts.RotateCharLiterals;

namespace Mecurl.Actors
{
    public class Player : Actor
    {
        public Player(in Loc pos) : base(pos, 100, '@', Color.Wheat)
        {
            Name = "Player";
            Direction initialFacing = Direction.N;

            Option<ICommand> fire(Weapon w)
            {
                WeaponGroup wg = ((Actor)Game.Player).WeaponGroup;

                Console.WriteLine("firin' the nukes");
                Game.StateHandler.PushState(new TargettingState(Game.MapHandler, this, Measure.Euclidean,
                    new TargetZone(TargetShape.Range, 20, 2), targets =>
                    {
                        Game.StateHandler.PopState();
                        wg.Advance(w.PrevGroup);
                        var explosionAnim = Option.Some<IAnimation>(new ExplosionAnimation(targets, Colors.Fire));
                        return Option.Some<ICommand>(new AttackCommand(this, 400, 10, targets, explosionAnim));
                    }));

                return Option.None<ICommand>();
            }

            WeaponGroup = new WeaponGroup();

            var reader = new RexReader("AsciiArt/missileLauncher.xp");
            var tilemap = reader.GetMap();

            PartHandler = new PartHandler(initialFacing, new List<Part>()
            {
                new Part(3, 3, new Loc(0, 0), initialFacing,
                    new RotateChar[9] { sr, b1, sl , b4, at, b3, sl, b2, sr }) { Name = "Core" },
                new Part(1, 2, new Loc(-2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}) { Name = "Leg" },
                new Part(1, 2, new Loc(2, 0), initialFacing,
                    new RotateChar[2] { arn, arn}) { Name = "Leg" },
                new Weapon(3, 3, new Loc(-2, 2), initialFacing,
                    new RotateChar[9] { b2, b4, sl, b2, b4, b4, sl, b2, b2 },
                    WeaponGroup, 0, fire) { Name = "Missiles (Left)", Art = tilemap  },
                new Weapon(3, 3, new Loc(2, 2), initialFacing,
                    new RotateChar[9] { sr, b4, b2, b4, b4, b2, b2, b2, sr },
                    WeaponGroup, 0, fire) { Name = "Missiles (Right)", Art = tilemap },
            });
        }

        // Commands processed in main loop
        public override Option<ICommand> GetAction()
        {
            return Option.None<ICommand>();
        }
    }
}
