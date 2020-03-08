using BearLib;

namespace Mecurl.Input
{
    internal enum NormalInput
    {
        None,
        Backward,
        Forward,
        StrafeRight,
        StrafeLeft,
        TurnLeft,
        TurnRight,
        WeaponGroup1,
        WeaponGroup2,
        WeaponGroup3,
        WeaponGroup4,
        WeaponGroup5,
        WeaponGroup6,
        Wait,
        UseCoolant,
    }

    internal static partial class InputMapping
    {
        public static NormalInput GetNormalInput(int key)
        {
            if (Terminal.Check(Terminal.TK_SHIFT))
            {
                if (_keyMap.NormalMap.Shift.TryGetValue(key, out NormalInput action))
                    return action;
            }
            else if (_keyMap.NormalMap.None.TryGetValue(key, out NormalInput action))
            {
                return action;
            }

            return NormalInput.None;
        }
    }
}
