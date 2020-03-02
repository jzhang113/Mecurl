using Optional;

namespace Engine
{
    public interface ICommand
    {
        // Visual effects when performing an Action.
        Option<IAnimation> Animation { get; }

        // Execute the Action and return an alternative if it fails. Returns None on success.
        Option<ICommand> Execute();
    }
}
