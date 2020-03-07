﻿using Engine;
using Engine.Drawing;
using Mecurl.Actors;
using Mecurl.Commands;
using Mecurl.Input;
using Mecurl.Parts;
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
            Mech player = (Mech)BaseGame.Player;
            
            switch (InputMapping.GetNormalInput(key))
            {
                case NormalInput.None:
                    return Option.None<ICommand>();

                #region Movement Keys
                case NormalInput.StrafeLeft:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + player.Facing.Left().Left()));
                case NormalInput.Backward:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos - player.Facing));
                case NormalInput.Forward:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + player.Facing));
                case NormalInput.StrafeRight:
                    return Option.Some<ICommand>(new MoveCommand(player, player.Pos + player.Facing.Right().Right()));
                case NormalInput.TurnLeft:
                    return Option.Some<ICommand>(new TurnCommand(player, Math.PI / 2));
                case NormalInput.TurnRight:
                    return Option.Some<ICommand>(new TurnCommand(player, -Math.PI / 2));
                case NormalInput.Wait:
                    return Option.Some<ICommand>(new WaitCommand(player));
                #endregion

                #region Weapon Group Firing;
                case NormalInput.WeaponGroup1:
                    return player.PartHandler.WeaponGroup.FireGroup(player, 0);
                case NormalInput.WeaponGroup2:
                    return player.PartHandler.WeaponGroup.FireGroup(player, 1);
                case NormalInput.WeaponGroup3:
                    return player.PartHandler.WeaponGroup.FireGroup(player, 2);
                case NormalInput.WeaponGroup4:
                    return player.PartHandler.WeaponGroup.FireGroup(player, 3);
                case NormalInput.WeaponGroup5:
                    return player.PartHandler.WeaponGroup.FireGroup(player, 4);
                case NormalInput.WeaponGroup6:
                    return player.PartHandler.WeaponGroup.FireGroup(player, 5);
                #endregion
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
