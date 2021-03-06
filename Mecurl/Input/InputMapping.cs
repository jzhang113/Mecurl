﻿using BearLib;
using Engine;
using Optional;
using System;
using System.Collections.Generic;

namespace Mecurl.Input
{
    internal static partial class InputMapping
    {
        private static readonly KeyMap _keyMap;

        static InputMapping()
        {
            _keyMap = new KeyMap()
            {
                NormalMap = new KeyMap.StateMap<NormalInput>()
                {
                    None = new Dictionary<int, NormalInput>()
                    {
                        [Terminal.TK_LEFT] =        NormalInput.MoveLeft,
                        [Terminal.TK_KP_4] =        NormalInput.MoveLeft,
                        [Terminal.TK_H] =           NormalInput.MoveLeft,
                        [Terminal.TK_DOWN] =        NormalInput.MoveDown,
                        [Terminal.TK_KP_2] =        NormalInput.MoveDown,
                        [Terminal.TK_J] =           NormalInput.MoveDown,
                        [Terminal.TK_UP] =          NormalInput.MoveUp,
                        [Terminal.TK_KP_8] =        NormalInput.MoveUp,
                        [Terminal.TK_K] =           NormalInput.MoveUp,
                        [Terminal.TK_RIGHT] =       NormalInput.MoveRight,
                        [Terminal.TK_KP_6] =        NormalInput.MoveRight,
                        [Terminal.TK_L] =           NormalInput.MoveRight,
                        [Terminal.TK_KP_7] =        NormalInput.TurnLeft,
                        [Terminal.TK_Y] =           NormalInput.TurnLeft,
                        [Terminal.TK_KP_9] =        NormalInput.TurnRight,
                        [Terminal.TK_U] =           NormalInput.TurnRight,
                        [Terminal.TK_KP_5] =        NormalInput.Wait,
                        [Terminal.TK_PERIOD] =      NormalInput.Wait,
                        [Terminal.TK_1] =           NormalInput.WeaponGroup1,
                        [Terminal.TK_2] =           NormalInput.WeaponGroup2,
                        [Terminal.TK_3] =           NormalInput.WeaponGroup3,
                        [Terminal.TK_4] =           NormalInput.WeaponGroup4,
                        [Terminal.TK_5] =           NormalInput.WeaponGroup5,
                        [Terminal.TK_6] =           NormalInput.WeaponGroup6,
                        [Terminal.TK_Z] =           NormalInput.UseCoolant,
                    },
                    Shift = new Dictionary<int, NormalInput>()
                    {
                        [Terminal.TK_LEFT] =        NormalInput.TurnLeft,
                        [Terminal.TK_RIGHT] =       NormalInput.TurnRight,
                    }
                },
                TargettingMap = new KeyMap.StateMap<TargettingInput>()
                {
                    None = new Dictionary<int, TargettingInput>()
                    {
                        [Terminal.TK_LEFT] =        TargettingInput.MoveW,
                        [Terminal.TK_KP_4] =        TargettingInput.MoveW,
                        [Terminal.TK_H] =           TargettingInput.MoveW,
                        [Terminal.TK_DOWN] =        TargettingInput.MoveS,
                        [Terminal.TK_KP_2] =        TargettingInput.MoveS,
                        [Terminal.TK_J] =           TargettingInput.MoveS,
                        [Terminal.TK_UP] =          TargettingInput.MoveN,
                        [Terminal.TK_KP_8] =        TargettingInput.MoveN,
                        [Terminal.TK_K] =           TargettingInput.MoveN,
                        [Terminal.TK_RIGHT] =       TargettingInput.MoveE,
                        [Terminal.TK_KP_6] =        TargettingInput.MoveE,
                        [Terminal.TK_L] =           TargettingInput.MoveE,
                        [Terminal.TK_KP_7] =        TargettingInput.MoveNW,
                        [Terminal.TK_Y] =           TargettingInput.MoveNW,
                        [Terminal.TK_KP_9] =        TargettingInput.MoveNE,
                        [Terminal.TK_U] =           TargettingInput.MoveNE,
                        [Terminal.TK_KP_1] =        TargettingInput.MoveSW,
                        [Terminal.TK_B] =           TargettingInput.MoveSW,
                        [Terminal.TK_KP_3] =        TargettingInput.MoveSE,
                        [Terminal.TK_N] =           TargettingInput.MoveSE,
                        [Terminal.TK_TAB] =         TargettingInput.NextActor,
                        [Terminal.TK_ENTER] =       TargettingInput.Fire,
                        [Terminal.TK_KP_ENTER] =    TargettingInput.Fire
                    },
                    Shift = new Dictionary<int, TargettingInput>()
                    {
                        [Terminal.TK_LEFT] =        TargettingInput.JumpW,
                        [Terminal.TK_KP_4] =        TargettingInput.JumpW,
                        [Terminal.TK_H] =           TargettingInput.JumpW,
                        [Terminal.TK_DOWN] =        TargettingInput.JumpS,
                        [Terminal.TK_KP_2] =        TargettingInput.JumpS,
                        [Terminal.TK_J] =           TargettingInput.JumpS,
                        [Terminal.TK_UP] =          TargettingInput.JumpN,
                        [Terminal.TK_KP_8] =        TargettingInput.JumpN,
                        [Terminal.TK_K] =           TargettingInput.JumpN,
                        [Terminal.TK_RIGHT] =       TargettingInput.JumpE,
                        [Terminal.TK_KP_6] =        TargettingInput.JumpE,
                        [Terminal.TK_L] =           TargettingInput.JumpE,
                        [Terminal.TK_KP_7] =        TargettingInput.JumpNW,
                        [Terminal.TK_Y] =           TargettingInput.JumpNW,
                        [Terminal.TK_KP_9] =        TargettingInput.JumpNE,
                        [Terminal.TK_U] =           TargettingInput.JumpNE,
                        [Terminal.TK_KP_1] =        TargettingInput.JumpSW,
                        [Terminal.TK_B] =           TargettingInput.JumpSW,
                        [Terminal.TK_KP_3] =        TargettingInput.JumpSE,
                        [Terminal.TK_N] =           TargettingInput.JumpSE
                    }
                },
            };
        }
    }
}
