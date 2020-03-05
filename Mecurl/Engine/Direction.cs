namespace Engine
{
    public enum Direction
    {
        N, E, S, W, NE, SE, SW, NW, Center
    }

    public static class DirectionExtensions
    {
        public static readonly Direction[] DirectionList = {
            Direction.N,
            Direction.E,
            Direction.S,
            Direction.W,
            Direction.NE,
            Direction.SE,
            Direction.SW,
            Direction.NW
        };

        public static void Deconstruct(this Direction dir, out int dx, out int dy)
        {
            (dx, dy) = dir switch
            {
                Direction.N => (0, -1),
                Direction.NE => (1, -1),
                Direction.E => (1, 0),
                Direction.SE => (1, 1),
                Direction.S => (0, 1),
                Direction.SW => (-1, 1),
                Direction.W => (-1, 0),
                Direction.NW => (-1, -1),
                _ => (0, 0),
            };
        }

        public static Direction Right(this Direction dir)
        {
            return dir switch
            {
                Direction.N => Direction.NE,
                Direction.NE => Direction.E,
                Direction.E => Direction.SE,
                Direction.SE => Direction.S,
                Direction.S => Direction.SW,
                Direction.SW => Direction.W,
                Direction.W => Direction.NW,
                Direction.NW => Direction.N,
                _ => Direction.Center,
            };
        }

        public static Direction Left(this Direction dir)
        {
            return dir switch
            {
                Direction.N => Direction.NW,
                Direction.NW => Direction.W,
                Direction.W => Direction.SW,
                Direction.SW => Direction.S,
                Direction.S => Direction.SE,
                Direction.SE => Direction.E,
                Direction.E => Direction.NE,
                Direction.NE => Direction.N,
                _ => Direction.Center,
            };
        }
    }
}
