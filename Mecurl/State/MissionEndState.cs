using BearLib;
using Engine;
using Engine.Drawing;
using Optional;
using System;

namespace Mecurl.State
{
    internal sealed class MissionEndState : IState
    {
        private bool _success;

        public MissionEndState(bool success)
        {
            _success = success;
        }

        public Option<ICommand> HandleKeyInput(int key)
        {
            if (_success)
            {
                switch (key)
                {
                    case Terminal.TK_ENTER:
                        Game.StateHandler.PopState();
                        Game.StateHandler.PopState();
                        break;
                }
            }
            else
            {
                switch (key)
                {
                    case Terminal.TK_ENTER:
                        Game.Reset();
                        Game.StateHandler.Reset();
                        break;
                }
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
