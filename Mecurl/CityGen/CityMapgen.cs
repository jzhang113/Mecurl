using Engine;
using Engine.Map;
using Priority_Queue;
using SimplexNoise;
using System;
using System.Collections.Generic;

namespace Mecurl.CityGen
{
    public class CityMapgen : MapGenerator
    {
        public CityMapgen(int width, int height, int level) : base(width, height, level, Game.Rand)
        {
        }

        protected override void CreateMap()
        {
            float[,] noiseMap = Noise.Calc2D(Width, Height, 0.1f);

            ProjectRoadsToMap(CreateRoads(noiseMap));
        }

        private void ProjectRoadsToMap(IEnumerable<Road> roads)
        {
            foreach (Road r in roads)
            {
                int startX = Math.Clamp((int)r.StartX, 0, Width - 1);
                int startY = Math.Clamp((int)r.StartY, 0, Height - 1);
                int endX = Math.Clamp((int)r.EndX, 0, Width - 1);
                int endY = Math.Clamp((int)r.EndY, 0, Height - 1);
                var startLoc = new Loc(startX, startY);
                var endLoc = new Loc(endX, endY);

                foreach (Loc loc in Map.GetStraightLinePath(startLoc, endLoc))
                {
                    foreach (Loc s in Map.GetPointsInRadius(loc, r.Width))
                    {
                        Map.Field[s].IsWall = false;
                    }
                }
            }
        }

        protected override void PlaceActors()
        {
            Game.Player.Pos = Map.GetRandomOpenPoint();
            Map.AddActor(Game.Player);
        }

        protected override void PlaceItems()
        {
        }

        private IEnumerable<Road> CreateRoads(float[,] populationMap)
        {
            // Algorithm from http://nothings.org/gamedev/l_systems.html
            var potentialRoads = new SimplePriorityQueue<Road>();
            var acceptedRoads = new List<Road>();

            var StartX = Rand.Next(20, EngineConsts.MAP_WIDTH - 20);
            var StartY = Rand.Next(20, EngineConsts.MAP_HEIGHT - 20);
            var Length = Rand.NextDouble() * 15 + 10;
            var Angle = Rand.NextDouble() * 2 * Math.PI;
            var r1 = new Road(0, StartX, StartY, Length, Angle, 3);
            var r2 = new Road(0, StartX, StartY, Length, Angle + Math.PI, 3);

            potentialRoads.Enqueue(r1, 0);
            potentialRoads.Enqueue(r2, 0);

            while (potentialRoads.Count > 0)
            {
                Road road = potentialRoads.First;
                float prio = potentialRoads.GetPriority(road);
                potentialRoads.Dequeue();

                if (CheckLocalConstraints(road, out Road newRoad))
                {
                    acceptedRoads.Add(newRoad);

                    foreach (Road rq in SolveGlobalGoals(newRoad))
                    {
                        if (rq != null)
                        {
                            potentialRoads.Enqueue(rq, prio + 1);
                        }
                    }
                }
            }

            return acceptedRoads;
        }

        private bool CheckLocalConstraints(Road road, out Road newRoad)
        {
            newRoad = road;

            if (road.Generation >= 4)
                return false;

            if (newRoad.StartX < 0 || newRoad.StartX >= Width)
                return false;

            if (newRoad.StartY < 0 || newRoad.StartY >= Height)
                return false;

            return true;
        }

        private IEnumerable<Road> SolveGlobalGoals(Road prevRoad)
        {
            // After every segment, we can continue straight, turn left, or turn right
            var newRoads = new Road[3];

            newRoads[0] = new Road(
                prevRoad.Generation, prevRoad.EndX, prevRoad.EndY, Rand.NextDouble() * 15 + 10,
                prevRoad.Angle + Rand.NextDouble() * Math.PI / 4 - Math.PI / 8, prevRoad.Width);

            if (Rand.NextDouble() > 0.8)
            {
                newRoads[1] = GenerateBranch(prevRoad, -Math.PI / 2);
            }

            if (Rand.NextDouble() > 0.8)
            {
                newRoads[2] = GenerateBranch(prevRoad, Math.PI / 2);
            }

            return newRoads;
        }

        private Road GenerateBranch(Road prevRoad, double offset)
        {
            int roadWidth;
            if (prevRoad.Generation == 0)
                roadWidth = prevRoad.Width - 1;
            else if (Rand.NextDouble() > 0.4)
                roadWidth = prevRoad.Width - 1;
            else
                roadWidth = prevRoad.Width;

            return new Road(
                prevRoad.Generation + 1, prevRoad.EndX, prevRoad.EndY, Rand.NextDouble() * 15 + 10,
                prevRoad.Angle + offset, roadWidth);
        }

        private class Road
        {
            public int Generation { get; }
            public double StartX { get; }
            public double StartY { get; }
            public double Length { get; }
            public double Angle { get; }
            public int Width { get; }

            public double EndX => StartX + Length * Math.Cos(Angle);
            public double EndY => StartY + Length * Math.Sin(Angle);

            public Road(int gen, double startX, double startY, double length, double angle, int width)
            {
                Generation = gen;
                StartX = startX;
                StartY = startY;
                Length = length;
                Angle = angle;
                Width = width;
            }
        }
    }
}
