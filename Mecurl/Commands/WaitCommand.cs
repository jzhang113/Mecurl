using Engine;
using Optional;

namespace Mecurl.Commands
{
    // Skip this turn
    internal class WaitCommand : ICommand
    {
        public ISchedulable Source { get; }
        public int TimeCost { get; }
        public Option<IAnimation> Animation => Option.None<IAnimation>();

        public WaitCommand(ISchedulable source)
        {
            Source = source;

            // TODO: need a "smart" wait based on other nearby actors
            TimeCost = EngineConsts.TURN_TICKS;
        }

        public Option<ICommand> Execute()
        {
            return Option.None<ICommand>();
        }
    }
}
