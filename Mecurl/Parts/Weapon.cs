using Engine;
using Mecurl.Actors;
using Optional;
using System;
using System.Collections.Generic;

namespace Mecurl.Parts
{
    public class Weapon : Part
    {        
        public Func<Weapon, Option<ICommand>> Activate { get; }

        internal int PrevGroup { get; set; }

        public Weapon(
            int width, int height, Loc center, Direction facing, RotateChar[] structure, double stability,
            WeaponGroup wg, int group, Func<Weapon, Option<ICommand>> activate) : base(width, height, center, facing, structure, stability)
        {
            PrevGroup = group;
            Activate = activate;

            wg.Add(this, group);
        }
    }
}
