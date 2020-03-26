using Engine;
using Mecurl.Actors;
using Optional;

namespace Mecurl.Commands
{
    // Skip this turn
    internal class WaitCommand : ICommand
    {
        public int TimeCost { get; }

        public WaitCommand(Mech source)
        {
            TimeCost = source.PartHandler.GetMoveSpeed();
        }

        public WaitCommand(int ticks)
        {
            TimeCost = ticks;
        }
    }
}
