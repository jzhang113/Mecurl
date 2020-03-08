using Engine;
using Mecurl.Actors;
using System;
using System.Collections.Generic;

namespace Mecurl.Parts
{
    public class Weapon : Part
    {        
        public TargetZone Target { get; }
        public Func<Mech, IEnumerable<Loc>, ICommand> Attack { get; }
        internal int Group { get; set; }

        public Weapon(
            int width, int height, Loc center, Direction facing, RotateChar[] structure, double stability,
            TargetZone target, Func<Mech, IEnumerable<Loc>, ICommand> attack) : base(width, height, center, facing, structure, stability)
        {
            Target = target;
            Attack = attack;

            Group = -1;
        }
    }
}
