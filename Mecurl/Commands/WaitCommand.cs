using Engine;
using Mecurl.Actors;
using Optional;

namespace Mecurl.Commands
{
    // Skip this turn
    internal class WaitCommand : ICommand
    {
        public int TimeCost { get; }
        public Option<IAnimation> Animation => Option.None<IAnimation>();

        public WaitCommand(Mech source)
        {
            TimeCost = source.PartHandler.GetMoveSpeed();
        }

        public WaitCommand(Mech source, int ticks)
        {
            TimeCost = ticks;
        }

        public Option<ICommand> Execute()
        {
            return Option.None<ICommand>();
        }
    }
}
