using System;

namespace Engine
{
    // Helper methods for calculating distances
    public static class Distance
    {
        public static Loc GetNearestDirection(in Loc a, in Loc b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
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
        public static double Euclidean(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(EuclideanSquared(x1, y1, x2, y2));
        }

        public static int EuclideanSquared(int x1, int y1, int x2, int y2)
        {
            int dx = x1 - x2;
            int dy = y1 - y2;
            return dx * dx + dy * dy;
        }

        public static int Chebyshev(in Loc pos1, in Loc pos2)
        {
            int dx = Math.Abs(pos1.X - pos2.X);
            int dy = Math.Abs(pos1.Y - pos2.Y);
            return Math.Max(dx, dy);
        }
    }
}
