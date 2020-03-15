using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Parts;
using Mecurl.Parts.Components;
using Optional;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Mecurl.State
{
    public class IntermissionState : IState
    {
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

        private static double[,] _perlinMap;
        private static double[,] _perlinMap2;

        public IntermissionState()
        {
            var _gen = new Perlin();
            _perlinMap = new double[95, 67];
            _perlinMap2 = new double[95, 67];
            for (int x = 0; x < 95; x++)
            {
                for (int y = 0; y < 67; y++)
                {
                    _perlinMap[x, y] = _gen.OctavePerlin(x * 0.2 + 5.3, y * 0.15 + 5.9, 14.44, 10, 0.5);
                    _perlinMap2[x, y] = _gen.OctavePerlin(x * 0.4 + 45.3, y * 0.6 + 11.2, 18.7, 10, 0.5);
                }
            }

            CorePart core = Game.AvailCores[0];
            Part w1 = Game.AvailParts[0];
            Part w2 = Game.AvailParts[1];

            var ph = new PartHandler(core);
            for (int i = 0; i < Game.AvailParts.Count; i++)
            {
                ph.Add(Game.AvailParts[i]);
            }
            ph.WeaponGroup.Add(w1, 0);
            ph.WeaponGroup.Add(w2, 0);

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
                        _hangar[_selectedIndex].WeaponGroup.Reassign(_selectedPart, num);
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
                        if (_selectedIndex == -1) break;
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
            PartHandler partHandler = _hangar[_selectedIndex];
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
            if (_selectedIndex != -1)
            {
                PartHandler ph = _hangar[_selectedIndex];

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
            Terminal.Layer(2);
            layer.Print(1, 1, $"Overworld map");
            Terminal.Layer(1);
            for (int i = 0; i < 95; i++)
            {
                for (int j = 0; j < 67; j++)
                {
                    if (_perlinMap[i, j] < 0.37)
                    {
                        Terminal.Color(Color.Blue);
                    }
                    else
                    {
                        Terminal.Color(Color.Green);
                    }

                    char c;
                    if (_perlinMap2[i, j] < 0.35)
                    {
                        c = ' ';
                    }
                    else if (_perlinMap2[i, j] < 0.4)
                    {
                        c = '░';
                    }
                    else if (_perlinMap2[i, j] < 0.47)
                    {
                        c = '▒';
                    }
                    else if (_perlinMap2[i, j] < 0.55)
                    {
                        c = '▓';
                    }
                    else
                    {
                        c = '█';
                    }

                    Terminal.Put(2 + i, 2 + j, c);
                }
            }

            // mission briefing
            int buttonBorderY = layer.Height - _bHeight - 5;
            int briefingBorderX = layer.Width - 40;

            Terminal.Color(Colors.Text);
            layer.Print(briefingBorderX + 1, 1, "Mission summary");
            layer.Print(briefingBorderX + 1, 2, "───────────────");

            string briefingString;
            if (Game.Difficulty >= 5)
                briefingString = "The region is peaceful. Congratulations, you have won.";
            else
                briefingString = "Hostile forces have been detected in the region";

            layer.Print(new Rectangle(briefingBorderX + 2, 3, 39, 28), briefingString, ContentAlignment.TopLeft);


            layer.Print(briefingBorderX + 1, 30, "Objectives");
            layer.Print(briefingBorderX + 1, 31, "──────────");

            string objectiveString = "None";
            switch (Game.NextMission.MissionType)
            {
                case MissionType.Elim: objectiveString = "Eliminate all enemies"; break;
            };
            if (Game.Difficulty >= 5)
                objectiveString = "None";
            layer.Print(briefingBorderX + 2, 32, objectiveString);

            layer.Print(briefingBorderX + 1, 60, "Reward");
            layer.Print(briefingBorderX + 1, 61, "──────");

            if (Game.Difficulty >= 5)
            {
                layer.Print(briefingBorderX + 2, 62, $"None");
            }
            else
            {
                int y = 62;
                if (Game.NextMission.RewardPart != null)
                    layer.Print(briefingBorderX + 2, y++, $"{Game.NextMission.RewardPart.Name}");

                if (Game.NextMission.RewardScrap > 0)
                    layer.Print(briefingBorderX + 2, y++, $"{Game.NextMission.RewardScrap} scrap");
            }

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
