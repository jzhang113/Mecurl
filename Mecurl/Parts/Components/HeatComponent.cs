namespace Mecurl.Parts.Components
{
    struct HeatComponent : IPartComponent
    {
        public double HeatGenerated { get; }
        public double HeatCapacity { get; }
        public double HeatRemoved { get; }
        public double MaxCoolant { get; }

        public HeatComponent(double heatGenerated, double heatCapacity, double heatRemoved, double maxCoolant)
        {
            HeatGenerated = heatGenerated;
            HeatCapacity = heatCapacity;
            HeatRemoved = heatRemoved;
            MaxCoolant = maxCoolant;
        }
    }
}
