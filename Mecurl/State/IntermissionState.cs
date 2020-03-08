using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Parts;
using Optional;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
        private bool _buildScreen = false;

        private int _selectedIndex = -1;
        private List<PartHandler> _hangar = new List<PartHandler>();
        private Part _selectedPart;
        private int _cursorX = 0;
        private int _cursorY = 0;
        private (int, int) _prevCursor = (0, 0);
        private string _message;
        private BuildState _bs = BuildState.None;

        private const int _buildCenterX = 70;
        private const int _buildCenterY = 40;

        private IntermissionState()
        {
            Core core = Game.AvailCores[0];
            var w1 = Game.AvailParts[0];
            var w2 = Game.AvailParts[1];

            var ph = new PartHandler
            {
                Core = core
            };
            ph.Add(core);
            ph.Add(w1);
            ph.Add(w2);
            ph.WeaponGroup.Add((Weapon)w1, 0);
            ph.WeaponGroup.Add((Weapon)w2, 0);

            _hangar.Add(ph);

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
                    _buildScreen = true;
                    _bBase.Disabled = false;
                    _bLoadout.Disabled = true;
                })
            {
                BackgroundColor = Color.FromArgb(152, 113, 61)
            };
            _bBase = new Button(new Rectangle(screenWidth / 2 + xOffset, yHeight, bWidth, _bHeight),
                "(F)ield base", () =>
                {
                    _buildScreen = false;
                    _bBase.Disabled = true;
                    _bLoadout.Disabled = false;
                }, disabled: true)
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
                    Game.Blueprint = (_selectedIndex == -1) ? _hangar[_selectedIndex] : _hangar[0];
                    Game.Blueprint.ValidateAndFix();
                    _bBegin.Press();
                    break;
            }

            if (_buildScreen)
            {
                int delta = Terminal.Check(Terminal.TK_SHIFT) ? 5 : 1;
                _message = "";

                switch (key)
                {
                    case Terminal.TK_A:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Add;
                        break;
                    case Terminal.TK_R:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Remove;
                        break;
                    case Terminal.TK_M:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Move;
                        break;
                    case Terminal.TK_O:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Rotate;
                        break;
                    case Terminal.TK_ESCAPE:
                        _bs = BuildState.None;
                        break;
                    case Terminal.TK_UP:
                        _cursorY = Math.Max(_cursorY - delta, -28);
                        break;
                    case Terminal.TK_DOWN:
                        _cursorY = Math.Min(_cursorY + delta, 28);
                        break;
                    case Terminal.TK_LEFT:
                        _cursorX = Math.Max(_cursorX - delta, -39);
                        break;
                    case Terminal.TK_RIGHT:
                        _cursorX = Math.Min(_cursorX + delta, 38);
                        break;
                    case Terminal.TK_1:
                    case Terminal.TK_2:
                    case Terminal.TK_3:
                    case Terminal.TK_4:
                    case Terminal.TK_5:
                    case Terminal.TK_6:
                    case Terminal.TK_7:
                    case Terminal.TK_8:
                    case Terminal.TK_9:
                    case Terminal.TK_0:
                        int corenum = key - Terminal.TK_1;
                        if (corenum < Game.AvailCores.Count)
                        {
                            _selectedIndex = corenum;
                        }
                        break;
                    case Terminal.TK_ENTER:
                        PerformBuildAction();
                        break;
                }
            }
            else
            {
            }

            return Option.None<ICommand>();
        }

        private void PerformBuildAction()
        {
            var partHandler = _hangar[_selectedIndex];
            switch (_bs)
            {
                case BuildState.None:
                    return;
                case BuildState.Add:
                    return;
                case BuildState.Remove:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part is Core)
                            _message = "Cannot remove core";
                        else
                            partHandler.Remove(part);
                    });                    
                    return;
                case BuildState.Move:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part is Core)
                            _message = "Cannot move core";
                        else
                        {
                            _selectedPart = part;
                            _prevCursor = (_cursorX, _cursorY);
                            _bs = BuildState.Drop;
                        }
                    });
                    return;
                case BuildState.Drop:
                    int dx = _cursorX - _prevCursor.Item1;
                    int dy = _cursorY - _prevCursor.Item2;
                    _selectedPart.Center += new Loc(dx, dy);
                    _selectedPart.UpdateBounds();
                    _bs = BuildState.None;
                    partHandler.Validate();
                    return;
                case BuildState.Rotate:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part is Core)
                            _message = "Cannot rotate core";
                        else
                        {
                            part.RotateLeft();
                            partHandler.Validate();
                        }
                    });
                    return;
            }
        }

        private Option<Part> GetCursorIntersect()
        {
            foreach (Part part in _hangar[_selectedIndex])
            {
                if (part.Bounds.Contains(_cursorX, _cursorY))
                    return Option.Some(part);
            }

            return Option.None<Part>();
        }

        public Option<ICommand> HandleMouseInput(double x, double y, ClickState click)
        {
            return Option.None<ICommand>();
        }

        public void Draw(LayerInfo layer)
        {
            if (_buildScreen)
            {
                DrawBuildScreen(layer);
            }
            else
            {
                DrawBriefingScreen(layer);
            }

            _bAbandon.Draw(layer);
            _bLoadout.Draw(layer);
            _bBase.Draw(layer);
            _bBegin.Draw(layer);
        }

        private void DrawBuildScreen(LayerInfo layer)
        {
            const int corePanelHeight = 8;
            const int sidePanelWidth = 30;

            // border locations
            int buttonBorderY = layer.Height - _bHeight - 5;
            int coreBorderY = corePanelHeight + 3;
            int infoBorderX = sidePanelWidth;
            int partBorderX = layer.Width - sidePanelWidth;

            // core window
            layer.Print(1, "Available mechs", ContentAlignment.TopCenter);
            layer.Print(2, "───────────────", ContentAlignment.TopCenter);

            int corePanelWidth = layer.Width - sidePanelWidth - sidePanelWidth - 1;
            int coreCount = Game.AvailCores.Count;
            const int dispCores = 10;

            for (int i = 0; i < Math.Min(dispCores, coreCount); i++)
            {
                double dispWidth = (double)corePanelWidth / dispCores;
                int xPos = infoBorderX + 1 + (int)(i * dispWidth);

                var core = Game.AvailCores[i];
                int yOffset = (corePanelHeight - core.Height) / 2;

                core.Draw(layer, new Loc(xPos + 2, yOffset + 3));

                if (i == _selectedIndex)
                {
                    layer.Print(xPos, coreBorderY - 1, "Current");
                }
                else
                {
                    char label = (i == 9) ? '0' : (char)('1' + i);
                    layer.Put(xPos, coreBorderY - 1, label);
                }
            }

            // info window
            layer.Print(new Rectangle(0, 1, infoBorderX - 1, 1), "Info", ContentAlignment.TopCenter);
            layer.Print(new Rectangle(0, 2, infoBorderX - 1, 2), "────", ContentAlignment.TopCenter);

            // part window
            layer.Print(new Rectangle(partBorderX, 1, 29, 1), "Avaiable Parts", ContentAlignment.TopCenter);
            layer.Print(new Rectangle(partBorderX, 2, 29, 2), "──────────────", ContentAlignment.TopCenter);

            int partPanelHeight = layer.Height - _bHeight - 1;
            int partCount = Game.AvailParts.Count;
            const int dispParts = 10;

            for (int i = 0; i < Math.Min(dispParts, partCount); i++)
            {
                double dispHeight = (double)partPanelHeight / dispParts;
                int yPos = 1 + (int)(i * dispHeight);

                var part = Game.AvailParts[i];
                part.Draw(layer, new Loc(partBorderX + 1, yPos));
                layer.Put(1, coreBorderY - 1, '0' + i);
            }

            // build window
            if (_selectedIndex != -1)
            {
                var ph = _hangar[_selectedIndex];

                ph.Draw(layer, new Loc(_buildCenterX, _buildCenterY), Colors.Player);

                Terminal.Color(_bs == BuildState.Add ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 1, buttonBorderY - 1, "(A)dd");
                Terminal.Color(_bs == BuildState.Remove ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 11, buttonBorderY - 1, "(R)emove");
                Terminal.Color(_bs == BuildState.Move || _bs == BuildState.Drop ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 24, buttonBorderY - 1, "(M)ove");
                Terminal.Color(_bs == BuildState.Rotate ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 35, buttonBorderY - 1, "R(o)tate");
            }
            else
            {
                Terminal.Color(Colors.Text);
                layer.Print(infoBorderX + 1, buttonBorderY - 1, "Select a mech to modify with the number keys");
            }

            // cursor
            if (_bs != BuildState.None)
            {
                if (_bs == BuildState.Drop)
                {
                    Rectangle bounds = _selectedPart.Bounds;
                    int dx = _cursorX - _prevCursor.Item1;
                    int dy = _cursorY - _prevCursor.Item2;
                    _selectedPart.Draw(layer, new Loc(_buildCenterX + dx, _buildCenterY + dy));
                }

                Terminal.Layer(2);
                Terminal.Color(Color.Red);
                layer.Put(_cursorX + _buildCenterX, _cursorY + _buildCenterY, '▒');
                Terminal.Layer(1);
            }

            Terminal.Color(Colors.Text);
            if (_selectedIndex != -1 && _hangar[_selectedIndex].Any(p => p.Invalid))
            {
                layer.Print(infoBorderX + 1, coreBorderY + 1, "Parts in red are invalid and will be removed");
                layer.Print(infoBorderX + 1, coreBorderY + 2, _message);
            }
            else
            {
                layer.Print(infoBorderX + 1, coreBorderY + 1, _message);
            }

            // borders and buttons
            Terminal.Color(Colors.BorderColor);

            for (int dx = 1; dx < layer.Width - 1; dx++)
            {
                layer.Put(dx, buttonBorderY, '═');
            }

            for (int dx = infoBorderX; dx < partBorderX; dx++)
            {
                layer.Put(dx, coreBorderY, '═');
            }

            for (int dy = 1; dy < buttonBorderY; dy++)
            {
                layer.Put(infoBorderX, dy, '║');
            }

            for (int dy = 1; dy < buttonBorderY; dy++)
            {
                layer.Put(partBorderX, dy, '║');
            }

            layer.Put(infoBorderX, coreBorderY, '╠');
            layer.Put(partBorderX, coreBorderY, '╣');
            layer.Put(infoBorderX, buttonBorderY, '╩');
            layer.Put(partBorderX, buttonBorderY, '╩');
        }

        private static void DrawBriefingScreen(LayerInfo layer)
        {
            // overworld map
            layer.Print(1, 1, "This is an overworld map");
            layer.Print(1, 2, "Imagine that you could select missions from here");

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
            for (int dx = 1; dx < layer.Width - 1; dx++)
            {
                layer.Put(dx, buttonBorderY, '═');
            }

            for (int dy = 1; dy < buttonBorderY; dy++)
            {
                layer.Put(briefingBorderX, dy, '║');
            }

            layer.Put(briefingBorderX, buttonBorderY, '╩');
        }

        private enum BuildState
        {
            None, Add, Remove, Move, Drop, Rotate
        }
    }
}
