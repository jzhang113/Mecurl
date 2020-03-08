using Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mecurl.Parts
{
    public class Core : Part
    {
        public double SpeedMultiplier { get; }

        public Core(
            int width, int height,
            Loc center, Direction facing,
            RotateChar[] structure,
            double stability, double speedMult) : base(width, height, center, facing, structure, stability)
        {
            SpeedMultiplier = speedMult;
        }
    }
}
