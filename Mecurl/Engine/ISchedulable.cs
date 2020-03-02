using Optional;

namespace Engine
{
    public interface ISchedulable
    {
        int Id { get; }
        string Name { get; }
        int Speed { get; }

        Option<ICommand> Act();
    }
}
