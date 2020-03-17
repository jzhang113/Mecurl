using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Parts;
using Mecurl.Parts.Components;
using Optional;
using System;
using System.Drawing;
using System.Linq;

namespace Mecurl.State
{
    class MechBuildState : IState
    {
        private const int _bHeight = 7;

        private Part _selectedPart;
        private int _cursorX = 0;
        private int _cursorY = 0;
        private (int, int) _prevCursor = (0, 0);
        private string _message;
        private BuildState _bs = BuildState.None;

        private int _addSelection = 0;

        private const int _buildCenterX = 70;
        private const int _buildCenterY = 40;

        private void PerformBuildAction()
        {
            PartHandler partHandler = Game.Hangar[Game.MechIndex];
            switch (_bs)
            {
                case BuildState.None:
                    return;
                case BuildState.AddSelect:
                    _selectedPart = Game.AvailParts[_addSelection];
                    if (partHandler.Contains(_selectedPart))
                    {
                        _message = "Part is already added";
                    }
                    else
                    {
                        _message = "Place the part by pressing enter";
                        _bs = BuildState.Add;
                    }
                    return;
                case BuildState.Add:
                    _selectedPart.Center = new Loc(_cursorX, _cursorY);
                    partHandler.Add(_selectedPart);
                    if (_selectedPart.Has<ActivateComponent>())
                    {
                        partHandler.WeaponGroup.Add(_selectedPart, 0);
                    }
                    _bs = BuildState.None;
                    partHandler.Validate();
                    return;
                case BuildState.Remove:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part.Has<CoreComponent>())
                            _message = "Cannot remove core";
                        else
                            partHandler.Remove(part);
                    });
                    return;
                case BuildState.Move:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part.Has<CoreComponent>())
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
                    _bs = BuildState.None;
                    partHandler.Validate();
                    return;
                case BuildState.Rotate:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part.Get<CoreComponent>().HasValue)
                        {
                            partHandler.RotateLeft();
                        }
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
                        part.Get<StabilityComponent>().Match(
                            some: comp =>
                            {
                                double repairAmt = comp.MaxStability - comp.Stability;
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
                            },
                            none: () =>
                            {
                                _message = $"{part.Name} doesn't need repairs";
                            });
                    });
                    return;
                case BuildState.AssignGroup:
                    GetCursorIntersect().MatchSome(part =>
                    {
                        if (part.Has<ActivateComponent>())
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

            foreach (Part part in Game.Hangar[Game.MechIndex])
            {
                if (part.Bounds.Contains(_cursorX, _cursorY))
                {
                    _selectedPart = part;
                    return Option.Some(part);
                }
            }

            return Option.None<Part>();
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

                Part core = Game.AvailCores[i];
                int yOffset = (corePanelHeight - core.Height) / 2;

                for (int x = 0; x < core.Bounds.Width; x++)
                {
                    for (int y = 0; y < core.Bounds.Height; y++)
                    {
                        int boundsIndex = core.BoundingIndex(x, y);
                        char c = core.GetPiece(boundsIndex);
                        layer.Put(xPos + 2 + core.Center.X + x - 1, yOffset + 3 + core.Center.Y + y - 1, c);
                    }
                }

                if (i == Game.MechIndex)
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

                int y = 7;
                _selectedPart.Get<StabilityComponent>().MatchSome(comp =>
                {
                    layer.Print(2, y++, $"Stability: {comp.Stability} / {comp.MaxStability}");
                });

                _selectedPart.Get<SpeedComponent>().MatchSome(comp =>
                {
                    layer.Print(2, y++, $"Speed: {-comp.SpeedDelta}");
                });

                _selectedPart.Get<HeatComponent>().MatchSome(comp =>
                {
                    layer.Print(2, y++, $"Heat Capacity: {comp.HeatCapacity}");
                    layer.Print(2, y++, $"Heat Generated: {comp.HeatGenerated}");
                    layer.Print(2, y++, $"Heat Removed: {comp.HeatRemoved}");
                    layer.Print(2, y++, $"Coolant: {comp.MaxCoolant}");
                });

                _selectedPart.Get<ActivateComponent>().MatchSome(comp =>
                {
                    y++;
                    layer.Print(1, y++, "Weapon Data");
                    layer.Print(2, y++, $"Range: {comp.Target.Range}");
                    layer.Print(2, y++, $"Radius: {comp.Target.Radius}");
                    layer.Print(2, y++, $"Shape: {comp.Target.Shape}");
                    layer.Print(2, y++, $"Weapon Group: {comp.Group + 1}");
                });
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
                Part part = Game.AvailParts[i];
                if (partsYPos + part.Height > partPanelHeight) break;

                Terminal.Color(Colors.Player);
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
            if (Game.MechIndex != -1)
            {
                PartHandler ph = Game.Hangar[Game.MechIndex];

                Terminal.Layer(2);
                foreach (Part part in ph)
                {
                    Terminal.Color(part.Invalid ? Color.Red : Colors.Player);
                    for (int x = 0; x < part.Bounds.Width; x++)
                    {
                        for (int y = 0; y < part.Bounds.Height; y++)
                        {
                            int boundsIndex = part.BoundingIndex(x, y);
                            char c = part.GetPiece(boundsIndex);
                            layer.Put(_buildCenterX + part.Bounds.Left + x, _buildCenterY + part.Bounds.Top + y, c);
                        }
                    }
                }
                Terminal.Layer(1);

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

                    for (int x = 0; x < _selectedPart.Bounds.Width; x++)
                    {
                        for (int y = 0; y < _selectedPart.Bounds.Height; y++)
                        {
                            int boundsIndex = _selectedPart.BoundingIndex(x, y);
                            char c = _selectedPart.GetPiece(boundsIndex);
                            layer.Put(_buildCenterX + _selectedPart.Bounds.Left + dx + x, _buildCenterY + _selectedPart.Bounds.Top + dy + y, c);
                        }
                    }
                }

                Terminal.Layer(2);
                Terminal.Color(Color.Red);
                layer.Put(_cursorX + _buildCenterX, _cursorY + _buildCenterY, '▒');
                Terminal.Layer(1);
            }

            Terminal.Color(Colors.Text);
            if (Game.MechIndex != -1 && Game.Hangar[Game.MechIndex].Any(p => p.Invalid))
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

        public Option<ICommand> HandleKeyInput(int key)
        {
            int delta = Terminal.Check(Terminal.TK_SHIFT) ? 5 : 1;
            _message = "";

            if (_bs == BuildState.RepairConfirm)
            {
                if (key == Terminal.TK_Y || key == Terminal.TK_ENTER)
                {
                    _selectedPart.Get<StabilityComponent>().Match(
                        some: comp =>
                        {
                            double repairAmt = comp.MaxStability - comp.Stability;
                            double cost = Math.Min(Game.Scrap, EngineConsts.REPAIR_COST * repairAmt);

                            if (Game.Scrap > cost)
                            {
                                Game.Scrap -= cost;
                                comp.Stability = comp.MaxStability;
                            }
                            else
                            {
                                double actualRepaired = cost / EngineConsts.REPAIR_COST;
                                comp.Stability += actualRepaired;
                                Game.Scrap = 0;
                            }

                            _message = $"{_selectedPart.Name} repaired";
                        },
                        none: () =>
                        {
                            _message = $"Couldn't repair {_selectedPart.Name}";
                        });
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
                    Game.Hangar[Game.MechIndex].WeaponGroup.Reassign(_selectedPart, num);
                    _message = $"{_selectedPart.Name} assigned to weapon group {num + 1}";
                }
                else
                {
                    _message = "Nevermind";
                }

                _bs = BuildState.None;
            }

            // build actions
            if (Game.MechIndex != -1)
            {
                switch (key)
                {
                    case Terminal.TK_A:
                        _bs = BuildState.AddSelect;
                        _selectedPart = Game.AvailParts[_addSelection];
                        break;
                    case Terminal.TK_R:
                        _bs = BuildState.Remove;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_M:
                        _bs = BuildState.Move;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_O:
                        _bs = BuildState.Rotate;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_E:
                        _bs = BuildState.Repair;
                        GetCursorIntersect();
                        break;
                    case Terminal.TK_G:
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
                    case Terminal.TK_ENTER:
                        PerformBuildAction();
                        break;
                }
            }

            // mech select
            switch (key)
            {
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
                        Game.MechIndex = corenum;
                    }
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
            DrawBuildScreen(layer);
        }

        private enum BuildState
        {
            None, AddSelect, Add, Remove, Move, Drop, Rotate, Repair, RepairConfirm,
            AssignGroup, AssignGroupConfirm
        }
    }
}
