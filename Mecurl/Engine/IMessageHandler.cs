namespace Mecurl.Engine
{
    public interface IMessageHandler
    {
        // Place a new message onto the message log
        void Add(string text);

        // Modify the last message by adding additional text.
        void Append(string text);

        // Clear the message log
        void Clear();
    }
}
