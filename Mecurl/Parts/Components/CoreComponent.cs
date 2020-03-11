namespace Mecurl.Parts.Components
{
    struct CoreComponent : IPartComponent
    {
        public double SpeedMultiplier { get; }
        public double ArmorMultiplier { get; }

        public CoreComponent(double speedMult, double armorMult)
        {
            SpeedMultiplier = speedMult;
            ArmorMultiplier = armorMult;
        }
    }
}
