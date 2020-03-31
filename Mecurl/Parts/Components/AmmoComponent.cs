namespace Mecurl.Parts.Components
{
    class AmmoComponent : IPartComponent
    {
        public int Capacity { get; }
        public int Loaded { get; internal set; }
        public int Remaining { get; internal set; }
        public int Reload { get; }

        public AmmoComponent(int capacity, int total, int reload)
        {
            Capacity = capacity;
            Loaded = capacity;
            Remaining = total - capacity;
            Reload = reload;
        }
    }
}
