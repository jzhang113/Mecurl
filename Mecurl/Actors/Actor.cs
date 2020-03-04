using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Mecurl.Parts;
using Optional;
using System;
using System.Collections.Generic;
using System.Drawing;

using static Mecurl.Parts.RotateCharLiterals;

namespace Mecurl.Actors
{
    public class Actor : BaseActor
    {
        public PartHandler PartHandler { get; }
        public Loc Facing => PartHandler.Facing;

        public Actor(in Loc pos, int hp, char symbol, Color color) : base(pos, hp, symbol, color)
        {
            Loc initialFacing = Direction.N;

            PartHandler = new PartHandler(initialFacing, new List<Part>()
            {
                new Part(3, 3, new Loc(0, 0), initialFacing,              
                    new RotateChar[9] { sr, b1, sl , b4, at, b3, sl, b2, sr } ),
                new Part(2, 5, new Loc(-2, 0), initialFacing,
                    new RotateChar[10] { arn, arn, arn, arn, arn, arn, arn, arn, arn, arn}),
                new Part(2, 5, new Loc(3, 0), initialFacing,
                    new RotateChar[10] { arn, arn, arn, arn, arn, arn, arn, arn, arn, arn}),

            });
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

        public override void Draw(LayerInfo layer)
        {
            if (!ShouldDraw)
                return;

            Terminal.Color(Color);
            PartHandler.Draw(layer, Pos);            
        }
    }
}
