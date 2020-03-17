using BearLib;
using Engine;
using Engine.Drawing;
using Optional;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.State
{
    class IntermissionFrameState : IState
    {
        private const int _bHeight = 7;
        private readonly Button _bAbandon;
        private readonly Button _bLoadout;
        private readonly Button _bBase;
        private readonly Button _bBegin;

        private readonly IList<IState> _substates;
        private int _index;

        public IntermissionFrameState(IList<IState> substates)
        {
            _substates = substates;
            _index = 0;

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
                "(L)eave mission", () => Game.StateHandler.PopState())
            {
                BackgroundColor = Swatch.Compliment
            };
            _bLoadout = new Button(new Rectangle(screenWidth / 4 + xOffset, yHeight, bWidth, _bHeight),
                "(C)hange loadout", () =>
                {
                    _index = 1;
                    _bBase.Disabled = false;
                    _bLoadout.Disabled = true;
                })
            {
                BackgroundColor = Color.FromArgb(152, 113, 61)
            };
            _bBase = new Button(new Rectangle(screenWidth / 2 + xOffset, yHeight, bWidth, _bHeight),
                "(F)ield base", () =>
                {
                    _index = 0;
                    _bBase.Disabled = true;
                    _bLoadout.Disabled = false;
                }, disabled: true)
            {
                BackgroundColor = Color.FromArgb(152, 113, 61)
            };
            _bBegin = new Button(new Rectangle(screenWidth * 3 / 4 + xOffset, yHeight, bWidth, _bHeight),
                "(B)egin mission", () => Game.NewMission(Game.NextMission))
            {
                BackgroundColor = Color.FromArgb(57, 128, 0)
            };
        }

        public Option<ICommand> HandleKeyInput(int key)
        {
            switch (key)
            {
                case Terminal.TK_L:
                    _bAbandon.Press();
                    break;
                case Terminal.TK_C:
                    _bLoadout.Press();
                    break;
                case Terminal.TK_F:
                    _bBase.Press();
                    break;
                case Terminal.TK_B:
                    Game.Blueprint = (Game.MechIndex == -1) ? Game.Hangar[0] : Game.Hangar[Game.MechIndex];
                    Game.Blueprint.ValidateAndFix();
                    _bBegin.Press();
                    break;
            }

            return _substates[_index].HandleKeyInput(key);
        }

        public Option<ICommand> HandleMouseInput(double x, double y, ClickState click)
        {
            return Option.None<ICommand>();
        }

        public void Draw(LayerInfo layer)
        {
            _substates[_index].Draw(layer);

            _bAbandon.Draw(layer);
            _bBase.Draw(layer);
            _bBegin.Draw(layer);
            _bLoadout.Draw(layer);
        }
    }
}
