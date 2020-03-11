namespace Mecurl.Parts.Components
{
    class StabilityComponent : IPartComponent
    {
        public double MaxStability { get; }
        public double Stability { get; set; }

        public StabilityComponent(double stability)
        {
            MaxStability = stability;
            Stability = stability;
        }
    }
}
