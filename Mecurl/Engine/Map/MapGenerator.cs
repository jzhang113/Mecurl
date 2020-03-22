using Optional;
using System;
using System.Collections.Generic;

namespace Engine.Map
{
    public abstract class MapGenerator
    {
        protected int Width { get; }
        protected int Height { get; }
        protected Random Rand { get; }
        protected BaseMapHandler Map { get; }

        protected MapGenerator(BaseMapHandler map, Random random)
        {
            Map = map;
            Rand = random;

            Width = map.Width;
            Height = map.Height;
        }

        public BaseMapHandler Generate()
        {
            CreateMap();
            ComputeClearance();

            //PlaceStairs();

            return Map;
        }

        protected abstract void CreateMap();

        // Calculate and save how much space is around each square
        private void ComputeClearance()
        {
            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    int d;
                    const int clearanceLimit = 10; // limit maxclearance for perf

                    int maxClearance = Math.Min(Map.Width - x, Map.Height - y);
                    maxClearance = Math.Min(clearanceLimit, maxClearance);

                    for (d = 0; d < maxClearance; d++)
                    {
                        for (int c = 0; c <= d; c++)
                        {
                            if (Map.Field[x + c, y + d].IsWall || Map.Field[x + d, y + c].IsWall)
                                goto done;
                        }
                    }

                    done:
                    Map.Clearance[x, y] = d;
                }
            }
        }

        protected void CreateRoom(Room room)
        {
            for (int i = room.Left; i < room.Right; i++)
            {
                for (int j = room.Top; j < room.Bottom; j++)
                {
                    // Don't excavate the edges of the map
                    if (PointOnMap(i, j))
                        Map.Field[i, j].IsWall = false;
                }
            }
        }

        // Similar to CreateRoom, but doesn't leave a border.
        protected void CreateRoomWithoutBorder(Room room)
        {
            for (int i = room.Left; i <= room.Right; i++)
            {
                for (int j = room.Top; j <= room.Bottom; j++)
                {
                    if (PointOnMap(i, j))
                        Map.Field[i, j].IsWall = false;
                }
            }
        }

        protected void CreateHallway(int x1, int y1, int x2, int y2)
        {
            int dx = Math.Abs(x1 - x2);
            int dy = Math.Abs(y1 - y2);

            if (x1 < x2)
            {
                if (y1 < y2)
                {
                    CreateRoom(new Room(x2 - dx, y2, dx + 1, 1));
                    CreateRoom(new Room(x1, y1, 1, dy + 1));
                }
                else
                {
                    CreateRoom(new Room(x1, y1, dx + 1, 1));
                    CreateRoom(new Room(x2, y2, 1, dy + 1));
                }
            }
            else
            {
                if (y1 < y2)
                {
                    CreateRoom(new Room(x2, y2, dx + 1, 1));
                    CreateRoom(new Room(x1, y1, 1, dy + 1));
                }
                else
                {
                    CreateRoom(new Room(x1 - dx, y1, dx + 1, 1));
                    CreateRoom(new Room(x2, y2, 1, dy + 1));
                }
            }
        }

        private void PlaceStairs()
        {
            Map.Exit = Option.Some(Map.GetRandomOpenPoint());
        }

        protected bool PointOnMap(int x, int y)
        {
            return x > 0 && y > 0 && x < Width - 1 && y < Height - 1;
        }
    }
}
