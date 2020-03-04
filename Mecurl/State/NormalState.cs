using Engine;
using Engine.Drawing;
using Mecurl.Actors;
using Mecurl.Commands;
using Mecurl.Input;
using Optional;
using System;

namespace Mecurl.State
{
    internal sealed class NormalState : IState
    {
        private static readonly Lazy<NormalState> _instance = new Lazy<NormalState>(() => new NormalState());
        public static NormalState Instance => _instance.Value;

        private NormalState()
        {
        }

        // ReSharper disable once CyclomaticComplexity
        public Option<ICommand> HandleKeyInput(int key)
        {
            Actor player = (Actor)BaseGame.Player;

            if (Game._dead)
            {
                Game.StateHandler.Reset();
                return Option.None<ICommand>();
            }

            switch (InputMapping.GetNormalInput(key))
            {
                case NormalInput.None:
                    return Option.None<ICommand>();

                #region Movement Keys
                case NormalInput.MoveW:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + player.Facing.Left().Left()));
                case NormalInput.MoveS:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos - player.Facing));
                case NormalInput.MoveN:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + player.Facing));
                case NormalInput.MoveE:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + player.Facing.Right().Right()));
                case NormalInput.MoveNW:
                    player.RotateLeft();
                    return Option.None<ICommand>();
                case NormalInput.MoveNE:
                    player.RotateRight();
                    return Option.None<ICommand>();
                case NormalInput.Wait:

                    foreach (var p in player.PartHandler)
                    {
                        p.Health -= 10;
                    }

                    return Option.Some<ICommand>(new WaitCommand(player));
                #endregion

                //case NormalInput.Get:
                //    return _game.MapHandler.GetItem(player.Pos)
                //        .FlatMap(item => Option.Some<ICommand>(new PickupCommand(player, item)));
                case NormalInput.OpenMenu:
                    Game.Exit();
                    return Option.None<ICommand>();
            }

            return Option.None<ICommand>();
        }

        public Option<ICommand> HandleMouseInput(double x, double y, ClickState click)
        {
            return Option.None<ICommand>();
        }

        public void Draw(LayerInfo layer) => Game.MapHandler.Draw(layer);
    }
}
