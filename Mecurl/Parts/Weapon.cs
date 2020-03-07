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

        internal int Group { get; set; }

        public Weapon(
            int width, int height, Loc center, Direction facing, RotateChar[] structure, double stability,
            Func<Weapon, Option<ICommand>> activate) : base(width, height, center, facing, structure, stability)
        {
            Group = -1;
            Activate = activate;
        }
    }
}
