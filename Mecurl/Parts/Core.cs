using Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mecurl.Parts
{
    public class Core : Part
    {
        public double SpeedMultiplier { get; }
        public double MaxCoolant { get; }
        public double Coolant { get; internal set; }

        public Core(
            int width, int height,
            Loc center, Direction facing,
            RotateChar[] structure,
            double stability, double speedMult, double coolant) : base(width, height, center, facing, structure, stability)
        {
            SpeedMultiplier = speedMult;
            MaxCoolant = coolant;
            Coolant = coolant;
        }
    }
}
