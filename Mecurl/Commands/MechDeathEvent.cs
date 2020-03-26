using Engine;
using Mecurl.Actors;

namespace Mecurl.Commands
{
    class MechDeathEvent : ICommand
    {
        public Mech Source { get; }        
        public int TimeCost => 0;

        public MechDeathEvent(Mech source)
        {
            Source = source;
        }
    }
}
