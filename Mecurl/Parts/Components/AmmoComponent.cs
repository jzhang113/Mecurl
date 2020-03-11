namespace Mecurl.Parts.Components
{
    class AmmoComponent : IPartComponent
    {
        public int Capacity { get; }
        public int Remaining { get; internal set; }

        public AmmoComponent(int capacity, int remaining)
        {
            Capacity = capacity;
            Remaining = remaining;
        }
    }
}
