namespace Mecurl.Parts.Components
{
    struct SpeedComponent : IPartComponent
    {
        public int SpeedDelta { get; }

        public SpeedComponent(int speedDelta)
        {
            SpeedDelta = speedDelta;
        }
    }
}
