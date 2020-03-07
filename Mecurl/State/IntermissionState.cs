using BearLib;
using Engine;
using Engine.Drawing;
using Optional;
using System;
using System.Drawing;

namespace Mecurl.State
{
    public class IntermissionState : IState
    {
        private static readonly Lazy<IntermissionState> _instance = new Lazy<IntermissionState>(() => new IntermissionState());
        public static IntermissionState Instance => _instance.Value;

        private const int _bHeight = 7;
        private readonly Button _bAbandon;
        private readonly Button _bLoadout;
        private readonly Button _bBase;
        private readonly Button _bBegin;

        private IntermissionState()
        {
            // HACK: states should have access to their layers / width / height
            int screenWidth = EngineConsts.SCREEN_WIDTH + 2;
            int screenHeight = EngineConsts.SCREEN_HEIGHT + 2;



            // setup buttons
            double quarterWidth = (double)screenWidth / 4;
            double buttonWidth = quarterWidth - 4;

            int bWidth = (int)buttonWidth;
            int xOffset = (int)((quarterWidth - buttonWidth) / 2);
            int yHeight = screenHeight - _bHeight - xOffset;

            _bAbandon = new Button(new Rectangle(xOffset, yHeight, bWidth, _bHeight),
                "(A)bandon mission", () => Game.StateHandler.PopState())
            {
                BackgroundColor = Swatch.Compliment
            };
            _bLoadout = new Button(new Rectangle(screenWidth / 4 + xOffset, yHeight, bWidth, _bHeight),
                "(C)hange loadout", () => { })
            {
                BackgroundColor = Color.FromArgb(152, 113, 61)
            };
            _bBase = new Button(new Rectangle(screenWidth / 2 + xOffset, yHeight, bWidth, _bHeight),
                "(F)ield base", () => { }, disabled: true)
            {
                BackgroundColor = Color.FromArgb(152, 113, 61)
            };
            _bBegin = new Button(new Rectangle(screenWidth * 3 / 4 + xOffset, yHeight, bWidth, _bHeight),
                "(B)egin mission", () => Game.NewMission())
            {
                BackgroundColor = Color.FromArgb(57, 128, 0)
            };
        }

        public Option<ICommand> HandleKeyInput(int key)
        {
            switch (key)
            {
                case Terminal.TK_A:
                    _bAbandon.Press();
                    break;
                case Terminal.TK_C:
                    _bLoadout.Press();
                    break;
                case Terminal.TK_F:
                    _bBase.Press();
                    break;
                case Terminal.TK_B:
                    _bBegin.Press();
                    break;
            }

            return Option.None<ICommand>();
        }

        public Option<ICommand> HandleMouseInput(double x, double y, ClickState click)
        {
            return Option.None<ICommand>();
        }

        public void Draw(LayerInfo layer)
        {
            // overworld map
            layer.Print(1, 1, "This is an overworld map");

            // mission briefing
            int buttonBorderY = layer.Height - _bHeight - 5;
            int briefingBorderX = layer.Width - 40;

            Terminal.Color(Colors.Text);
            layer.Print(briefingBorderX + 1, 1, "Mission summary");
            layer.Print(briefingBorderX + 1, 2, "───────────────");


            layer.Print(briefingBorderX + 1, 30, "Objectives");
            layer.Print(briefingBorderX + 1, 31, "──────────");

            layer.Print(briefingBorderX + 1, 60, "Reward");
            layer.Print(briefingBorderX + 1, 61, "──────");

            // borders and buttons
            Terminal.Color(Colors.BorderColor);
            for (int dy = 1; dy < buttonBorderY; dy++)
            {
                layer.Put(briefingBorderX, dy, '║');
            }

            for (int dx = 1; dx < layer.Width - 1; dx++)
            {
                layer.Put(dx, buttonBorderY, '═');
            }

            layer.Put(briefingBorderX, buttonBorderY, '╩');

            _bAbandon.Draw(layer);
            _bLoadout.Draw(layer);
            _bBase.Draw(layer);
            _bBegin.Draw(layer);
        }
    }
}
