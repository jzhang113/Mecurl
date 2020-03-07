using System;

namespace Engine
{
    public enum Measure
    {
        Chebyshev, Manhatten, Euclidean
    }

    // Helper methods for calculating distances
    public static class Distance
    {
        public static Direction GetNearestDirection(in Loc to, in Loc from)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            int ax = Math.Abs(dx);
            int ay = Math.Abs(dy);
            bool straight = Math.Abs(ax - ay) > Math.Max(ax / 2, ay / 2);
            int sx = 0, sy = 0;

            if (straight)
            {
                if (ax > ay)
                    sx = Math.Sign(dx);
                else
                    sy = Math.Sign(dy);
            }
            else
            {
                sx = Math.Sign(dx);
                sy = Math.Sign(dy);
            }

            return (sx + 3 * sy + 5) switch
            {
                1 => Direction.NW,
                2 => Direction.N,
                3 => Direction.NE,
                4 => Direction.W,
                5 => Direction.Center,
                6 => Direction.E,
                7 => Direction.SW,
                8 => Direction.S,
                9 => Direction.SE,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static double Euclidean(in Loc pos1, in Loc pos2)
        {
            return Math.Sqrt(EuclideanSquared(pos1, pos2));
        }

        public static int EuclideanSquared(in Loc pos1, in Loc pos2)
        {
            int dx = pos1.X - pos2.X;
            int dy = pos1.Y - pos2.Y;
            return dx * dx + dy * dy;
        }

        public static int Chebyshev(in Loc pos1, in Loc pos2)
        {
            int dx = Math.Abs(pos1.X - pos2.X);
            int dy = Math.Abs(pos1.Y - pos2.Y);
            return Math.Max(dx, dy);
        }

        public static int Manhatten(in Loc pos1, in Loc pos2)
        {
            int dx = Math.Abs(pos1.X - pos2.X);
            int dy = Math.Abs(pos1.Y - pos2.Y);
            return dx + dy;
        }
    }
}
