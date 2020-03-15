using Engine;
using Mecurl.Parts.Components;

namespace Mecurl.Parts
{
    class CorePart : Part
    {
        public CoreComponent CoreComponent { get; }
        public StabilityComponent StabilityComponent { get; }

        public CorePart(string name, int width, int height, RotateChar[] structure, CoreComponent cc, StabilityComponent sc, Direction facing = Direction.N)
            : base(name, width, height, structure, facing)
        {
            CoreComponent = cc;
            StabilityComponent = sc;

            Components.Add(cc);
            Components.Add(sc);
        }
    }
}
