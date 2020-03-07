﻿using Engine;
using Mecurl.Parts;
using Optional;
using System;

namespace Mecurl.Actors
{
    public class Player : Mech
    {
        public Player(in Loc pos) : base(pos, 100, '@', Colors.Player)
        {
            Name = "Player";
            _messages = Game.MessagePanel;
        }

        // Commands processed in main loop
        public override Option<ICommand> GetAction()
        {
            return Option.None<ICommand>();
        }

        public override Option<ICommand> TriggerDeath()
        {
            Game.GameOver();
            return Option.None<ICommand>();
        }
    }
}
