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
        private readonly List<PartHandler> _hangar = new List<PartHandler>();
        private Part _selectedPart;
        private int _cursorX = 0;
        private int _cursorY = 0;
        private (int, int) _prevCursor = (0, 0);
        private string _message;
        private BuildState _bs = BuildState.None;
        private int _addSelection = 0;

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
                    Game.Blueprint = (_selectedIndex == -1) ? _hangar[0] : _hangar[_selectedIndex];
                    Game.Blueprint.ValidateAndFix();
                    _bBegin.Press();
                    break;
            }

            if (_buildScreen)
            {
                int delta = Terminal.Check(Terminal.TK_SHIFT) ? 5 : 1;
                _message = "";

                if (_bs == BuildState.RepairConfirm)
                {
                    if (key == Terminal.TK_Y || key == Terminal.TK_ENTER)
                    {
                        double repairAmt = _selectedPart.MaxStability - _selectedPart.Stability;
                        double cost = Math.Min(Game.Scrap, EngineConsts.REPAIR_COST * repairAmt);

                        if (Game.Scrap >= cost)
                        {
                            Game.Scrap -= cost;
                            _selectedPart.Stability = _selectedPart.MaxStability;
                        }
                        else
                        {
                            double actualRepaired = cost / EngineConsts.REPAIR_COST;
                            _selectedPart.Stability += actualRepaired;
                            Game.Scrap = 0;
                        }

                        _message = $"{_selectedPart.Name} repaired";
                    }
                    else
                    {
                        _message = "Nevermind";
                    }

                    _bs = BuildState.None;
                }

                if (_bs == BuildState.AssignGroupConfirm)
                {
                    int num = key - Terminal.TK_1;
                    if (num >= 0 && num <= 5)
                    {
                        _hangar[_selectedIndex].WeaponGroup.Reassign((Weapon)_selectedPart, num);
                        _message = $"{_selectedPart.Name} assigned to weapon group {num + 1}";
                    }
                    else
                    {
                        _message = "Nevermind";
                    }

                    _bs = BuildState.None;
                }

                switch (key)
                {
                    case Terminal.TK_A:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.AddSelect;
                        _selectedPart = Game.AvailParts[_addSelection];
                        break;
                    case Terminal.TK_R:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Remove;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_M:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Move;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_O:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Rotate;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_E:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.Repair;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_G:
                        if (_selectedIndex == -1) break;
                        _bs = BuildState.AssignGroup;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_ESCAPE:
                        _bs = BuildState.None;
                        break;
                    case Terminal.TK_UP:
                        if (_bs == BuildState.AddSelect)
                        {
                            _addSelection = Math.Max(_addSelection - 1, 0);
                            _selectedPart = Game.AvailParts[_addSelection];
                        }
                        else if (_bs != BuildState.None)
                        {
                            _cursorY = Math.Max(_cursorY - delta, -28);
                            if (_bs != BuildState.Add && _bs != BuildState.Drop) GetCursorIntersect();
                        }
                        break;
                    case Terminal.TK_DOWN:
                        if (_bs == BuildState.AddSelect)
                        {
                            _addSelection = Math.Min(_addSelection + 1, Game.AvailParts.Count - 1);
                            _selectedPart = Game.AvailParts[_addSelection];
                        }
                        else if (_bs != BuildState.None)
                        {
                            _cursorY = Math.Min(_cursorY + delta, 28);
                            if (_bs != BuildState.Add && _bs != BuildState.Drop) GetCursorIntersect();
                        }
                        break;
                    case Terminal.TK_LEFT:
                        if (_bs != BuildState.None)
                        {
                            _cursorX = Math.Max(_cursorX - delta, -39);
                            if (_bs != BuildState.Add && _bs != BuildState.Drop) GetCursorIntersect();
                        }
                        break;
                    case Terminal.TK_RIGHT:
                        if (_bs != BuildState.None)
                        {
                            _cursorX = Math.Min(_cursorX + delta, 38);
                            if (_bs != BuildState.Add && _bs != BuildState.Drop) GetCursorIntersect();
                        }
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
                case BuildState.AddSelect:
                    _selectedPart = Game.AvailParts[_addSelection];
                    _bs = BuildState.Add;
                    return;
                case BuildState.Add:
                    _selectedPart.Center = new Loc(_cursorX, _cursorY);
                    _selectedPart.UpdateBounds();
                    partHandler.Add(_selectedPart);
                    _bs = BuildState.None;
                    partHandler.Validate();
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
                case BuildState.Repair:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        double repairAmt = part.MaxStability - part.Stability;
                        if (repairAmt == 0)
                        {
                            _message = $"{part.Name} doesn't need repairs";
                        }
                        else
                        {
                            double cost = Math.Min(Game.Scrap, EngineConsts.REPAIR_COST * repairAmt);
                            _message = $"Spend {cost} to repair {part.Name}?";
                            _bs = BuildState.RepairConfirm;
                        }
                    });
                    return;
                case BuildState.AssignGroup:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part is Weapon w)
                        {
                            _message = $"Assign {part.Name} to which weapon group";
                            _bs = BuildState.AssignGroupConfirm;
                        }
                        else
                        {
                            _message = $"{part.Name} can't be assigned to a weapon group";
                        }
                    });
                    return;
            }
        }

        private Option<Part> GetCursorIntersect()
        {
            _selectedPart = null;

            foreach (Part part in _hangar[_selectedIndex])
            {
                if (part.Bounds.Contains(_cursorX, _cursorY))
                {
                    _selectedPart = part;
                    return Option.Some(part);
                }
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

            if (_bs != BuildState.None && _selectedPart != null)
            {
                layer.Print(1, 4, "General Data");
                layer.Print(2, 6, $"{_selectedPart.Name}");
                layer.Print(2, 7, $"Stability: {_selectedPart.Stability} / {_selectedPart.MaxStability} ");
                layer.Print(2, 8, $"Speed: {-_selectedPart.SpeedDelta} ");
                layer.Print(2, 9, $"Heat Capacity: {_selectedPart.HeatCapacity} ");
                layer.Print(2, 10, $"Heat Generated: {_selectedPart.HeatGenerated} ");
                layer.Print(2, 11, $"Heat Removed: {_selectedPart.HeatRemoved} ");

                if (_selectedPart is Weapon w)
                {
                    layer.Print(1, 13, "Weapon Data");
                    layer.Print(2, 15, $"Range: {w.Target.Range}");
                    layer.Print(2, 16, $"Radius: {w.Target.Radius}");
                    layer.Print(2, 17, $"Shape: {w.Target.Shape}");
                    layer.Print(2, 18, $"Weapon Group: {w.Group + 1}");
                }
            }

            layer.Print(1, buttonBorderY - 1, $"Scrap: {Game.Scrap}");

            // part window
            layer.Print(new Rectangle(partBorderX, 1, 29, 1), "Available Parts", ContentAlignment.TopCenter);
            layer.Print(new Rectangle(partBorderX, 2, 29, 2), "──────────────", ContentAlignment.TopCenter);

            int partPanelHeight = layer.Height - _bHeight - 6;
            int partCount = Game.AvailParts.Count;
            int partsYPos = 3;

            for (int i = 0; i < partCount; i++)
            {
                var part = Game.AvailParts[i];
                if (partsYPos + part.Height > partPanelHeight) break;

                Terminal.Color(Game.Player.Color);
                for (int x = 0; x < part.Bounds.Width; x++)
                {
                    for (int y = 0; y < part.Bounds.Height; y++)
                    {
                        int boundsIndex = part.BoundingIndex(x, y);
                        char c = part.GetPiece(boundsIndex);
                        layer.Put(partBorderX + 1 + x, partsYPos + y, c);
                    }
                }

                Terminal.Color(_bs == BuildState.AddSelect && _addSelection == i ? Colors.HighlightColor : Colors.Text);
                layer.Print(layer.Width - part.Name.Length - 1, partsYPos, part.Name);
                partsYPos += part.Height + 1;
            }

            // build window
            if (_selectedIndex != -1)
            {
                var ph = _hangar[_selectedIndex];

                ph.Draw(layer, new Loc(_buildCenterX, _buildCenterY), Colors.Player);

                Terminal.Color(_bs == BuildState.AddSelect || _bs == BuildState.Add ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 1, buttonBorderY - 1, "(A)dd");
                Terminal.Color(_bs == BuildState.Remove ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 11, buttonBorderY - 1, "(R)emove");
                Terminal.Color(_bs == BuildState.Move || _bs == BuildState.Drop ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 24, buttonBorderY - 1, "(M)ove");
                Terminal.Color(_bs == BuildState.Rotate ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 35, buttonBorderY - 1, "R(o)tate");
                Terminal.Color(_bs == BuildState.Repair ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 48, buttonBorderY - 1, "R(e)pair");
                Terminal.Color(_bs == BuildState.AssignGroup ? Colors.HighlightColor : Colors.Text);
                layer.Print(infoBorderX + 61, buttonBorderY - 1, "Assign(G)roup");
            }
            else
            {
                Terminal.Color(Colors.Text);
                layer.Print(infoBorderX + 1, buttonBorderY - 1, "Select a mech to modify with the number keys");
            }

            // cursor
            if (_bs != BuildState.None && _bs != BuildState.AddSelect)
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
            None, AddSelect, Add, Remove, Move, Drop, Rotate, Repair, RepairConfirm,
            AssignGroup, AssignGroupConfirm
        }
    }
}
