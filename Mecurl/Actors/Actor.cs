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
                    new char[9] { '/', ' ', '\\' , '█', '@', '█', '\\', '█', '/' } ),
                new Part(3, 2, new Loc(0, -1), initialFacing,
                    new char[6] { '*', '*', '*', ' ', '*', ' '}),
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

            if (IsDead)
            {
                Terminal.Color(Swatch.DbOldBlood);
                layer.Put(Pos.X - Camera.X, Pos.Y - Camera.Y, '%');
                return;
            }

            Terminal.Color(Color);
            PartHandler.Draw(layer, Pos);            
        }
    }
}
