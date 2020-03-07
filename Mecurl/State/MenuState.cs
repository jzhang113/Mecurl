using BearLib;
using Engine;
using Engine.Drawing;
using Optional;
using System;

namespace Mecurl.State
{
    internal sealed class MenuState : IState
    {
        private static readonly Lazy<MenuState> _instance = new Lazy<MenuState>(() => new MenuState());
        public static MenuState Instance => _instance.Value;

        private MenuState()
        {
        }

        public Option<ICommand> HandleKeyInput(int key)
        {
            switch (key)
            {
                case Terminal.TK_ENTER:
                    Game.StateHandler.PushState(IntermissionState.Instance);
                    return Option.None<ICommand>();
                case Terminal.TK_ESCAPE:
                case Terminal.TK_Q:
                    Game.Exit();
                    return Option.None<ICommand>();
                default:
                    return Option.None<ICommand>();
            }
        }

        public Option<ICommand> HandleMouseInput(double x, double y, ClickState click)
        {
            return Option.None<ICommand>();
        }

        public void Draw(LayerInfo layer)
        {
            int x = 2;

            Terminal.Clear();
            layer.Print(x, 2, "MechRL");
            int y = 4;

            y++;
            layer.Print(x, y++, "Controls:");
            layer.Print(x, y++, "Arrow keys, or number pad to move");
            layer.Print(x, y++, "Shift-left or Shift-right to turn");
            layer.Print(x, y++, "1-6 to fire weapons");
            layer.Print(x, y++, "While casting, press [[Enter]] to confirm or [[Esc]] to cancer");
            layer.Print(x, y++, "[[Esc]] to quit to this menu");

            layer.Print(x, ++y, "Press [[Enter]] to start");
        }
    }
}
