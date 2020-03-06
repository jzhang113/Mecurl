using BearLib;
using Engine;
using Engine.Drawing;
using Optional;
using System;

namespace Mecurl.State
{
    internal sealed class GameOverState : IState
    {
        private static readonly Lazy<GameOverState> _instance = new Lazy<GameOverState>(() => new GameOverState());
        public static GameOverState Instance => _instance.Value;

        private GameOverState()
        {
        }

        public Option<ICommand> HandleKeyInput(int key)
        {
            switch (key)
            {
                case Terminal.TK_ENTER:
                    Game.StateHandler.Reset();
                    break;                
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
