using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Optional;
using System.Drawing;

namespace Mecurl.Actors
{
    public class Actor : BaseActor
    {
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

        public override void Draw(LayerInfo layer)
        {

            if (!ShouldDraw)
                return;

            if (IsDead)
            {
                Terminal.Color(Swatch.DbOldBlood);
                layer.Put(Pos.X - Camera.X, Pos.Y - Camera.Y, '%');
            }
            else
            {
                Terminal.Color(Color);
                layer.Put(Pos.X - Camera.X, Pos.Y - Camera.Y, Symbol);
            }
        }
    }
}
