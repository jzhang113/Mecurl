using Optional;

namespace Engine
{
    // Commands are expressions of intent - in other words, they capture any necessary information
    // to handle a particular action, but actual execution is left to specific systems (and the
    // handlers should be attached using EventScheduler.Subscribe)
    // A system may generate a replacement command - for example, movement often needs to do a
    // validity pre-check and may either generate a successful or failed variant
    public interface ICommand
    {
        // How long it takes to recover (aka speed) - baseline is EngineConsts.TURN_TICKS
        int TimeCost { get; }
    }
}
