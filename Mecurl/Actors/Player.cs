using BearLib;
using Engine;
using Mecurl.Commands;
using Mecurl.Parts;
using Mecurl.State;
using Optional;
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

            Option<ICommand> fire()
            {
                Console.WriteLine("firin' the nukes");
                Game.StateHandler.PushState(new TargettingState(Game.MapHandler, this,
                    new TargetZone(TargetShape.Range, 10, 1), targets =>
                    {
                        Game.StateHandler.PopState();
                        return Option.Some<ICommand>(new AttackCommand(this, 400, 10, targets));
                    }));

                return Option.None<ICommand>();
            }
            Input.InputMapping.UpdateMapping(Terminal.TK_Z, fire);

            PartHandler = new PartHandler(initialFacing, new List<Part>()
            {
                new Part(3, 3, new Loc(0, 0), initialFacing,
                    new RotateChar[9] { sr, b1, sl , b4, at, b3, sl, b2, sr },
                    () => Option.None<ICommand>() ) { Name = "Core" },
                new Part(2, 5, new Loc(-2, 0), initialFacing,
                    new RotateChar[10] { arn, arn, arn, arn, arn, arn, arn, arn, arn, arn},
                    fire) { Name = "Treads" },
                new Part(2, 5, new Loc(3, 0), initialFacing,
                    new RotateChar[10] { arn, arn, arn, arn, arn, arn, arn, arn, arn, arn},
                    () => Option.None<ICommand>() ) { Name = "Treads" },
            });
        }

        // Commands processed in main loop
        public override Option<ICommand> GetAction()
        {
            return Option.None<ICommand>();
        }
    }
}
