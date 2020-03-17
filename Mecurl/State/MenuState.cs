using BearLib;
using Engine;
using Engine.Drawing;
using Optional;
using System;
using System.Drawing;

namespace Mecurl.State
{
    internal sealed class MenuState : IState
    {
        private static readonly Lazy<MenuState> _instance = new Lazy<MenuState>(() => new MenuState());
        public static MenuState Instance => _instance.Value;

        private readonly Button _bStart;
        private readonly Button _bQuit;

        private MenuState()
        {
            _bStart = new Button(new Rectangle(2, EngineConsts.SCREEN_HEIGHT - 18, 11, 7),
                "(S)tart", () =>
                {
                    var substates = new IState[] { new IntermissionState(), new MechBuildState() };
                    var state = new IntermissionFrameState(substates);
                    Game.StateHandler.PushState(state);
                });
            _bQuit = new Button(new Rectangle(2, EngineConsts.SCREEN_HEIGHT - 10, 11, 7),
                "(Q)uit", () => Game.Exit());
        }

    public Option<ICommand> HandleKeyInput(int key)
    {
        switch (key)
        {
            case Terminal.TK_S:
            case Terminal.TK_ENTER:
                _bStart.Press();
                return Option.None<ICommand>();
            case Terminal.TK_ESCAPE:
            case Terminal.TK_Q:
                _bQuit.Press();
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
        layer.Print(x, y++, "Z to use coolant");
        layer.Print(x, y++, "1-6 to fire weapons");
        layer.Print(x, y++, "While casting, press [[Enter]] to confirm or [[Esc]] to cancel");

        _bStart.Draw(layer);
        _bQuit.Draw(layer);
    }
}
}
