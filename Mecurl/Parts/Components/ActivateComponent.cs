using Engine;
using Mecurl.Actors;
using System;
using System.Collections.Generic;

namespace Mecurl.Parts.Components
{
    class ActivateComponent : IPartComponent
    {
        public TargetZone Target { get; }
        public Func<Mech, IEnumerable<Loc>, ICommand> Activate { get; }
        public int Cooldown { get; }
        public int CurrentCooldown { get; internal set; }

        internal int Group { get; set; }

        public ActivateComponent(TargetZone target, Func<Mech, IEnumerable<Loc>, ICommand> activate, int cooldown)
        {
            Target = target;
            Activate = activate;
            Cooldown = cooldown;

            Group = -1;
            CurrentCooldown = 0;
        }
    }
}
