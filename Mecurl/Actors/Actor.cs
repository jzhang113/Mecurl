using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Mecurl.Parts;
using Optional;
using System.Drawing;

namespace Mecurl.Actors
{
    public class Actor : BaseActor
    {
        public PartHandler PartHandler { get; protected set; }
        public Direction Facing => PartHandler.Facing;

        public Actor(in Loc pos, int hp, char symbol, Color color) : base(pos, hp, symbol, color)
        {
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
