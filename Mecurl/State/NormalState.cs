﻿using Engine;
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
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.W));
                case NormalInput.MoveS:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.S));
                case NormalInput.MoveN:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.N));
                case NormalInput.MoveE:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.E));
                case NormalInput.MoveNW:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.NW));
                case NormalInput.MoveNE:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.NE));
                case NormalInput.MoveSW:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.SW));
                case NormalInput.MoveSE:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + Direction.SE));
                case NormalInput.Wait:
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
