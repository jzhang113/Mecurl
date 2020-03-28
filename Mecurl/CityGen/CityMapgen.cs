using Engine;
using Engine.Map;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.CityGen
{
    public class CityMapgen : MapGenerator
    {
        private readonly MissionInfo _missionInfo;

        public CityMapgen(BaseMapHandler map, MissionInfo info) : base(map, Game.Rand)
        {
            _missionInfo = info;
        }

        protected override void CreateMap()
        {
            //float[,] noiseMap = Noise.Calc2D(Width, Height, 0.1f);
            CreateRoads();

            // gonna abuse this array so we don't have to reallocate it
            // here 0 represents not visited while 1 represents visited
            var visited = new int[Width, Height];
            IEnumerable<List<Loc>> regions = FindIslands(visited);

            // now we are going to subdivide each island
            // visited represents which subregion each point belongs to
            foreach (List<Loc> island in regions)
            {
                // we throw out mini-islands
                if (island.Count <= 3)
                {
                    foreach (Loc loc in island)
                    {
                        Map.Field[loc].Terrain = TileType.Grass;
                    }
                }
                else
                {
                    IList<Loc> regionBounds = FindBoundary(island);
                    // in case FindBoundary fails (probably because FindIslands terminated early)
                    if (regionBounds.Count <= 1)
                        continue;

                    // we want each building to be around 15-20 tiles
                    // add 1 to avoid having no start points
                    const int expectedBuildingSize = 30;
                    int buildingCount = island.Count / expectedBuildingSize + 1;

                    var subregions = new List<Loc>[buildingCount];
                    var startPoints = new Loc[buildingCount];
                    var colorMap = new Color[buildingCount];

                    for (int i = 0; i < buildingCount; i++)
                    {
                        int startPointIndex = Rand.Next(0, regionBounds.Count - 1);
                        Loc startPoint = regionBounds[startPointIndex];
                        startPoints[i] = startPoint;
                        colorMap[i] = Colors.RandomColor();

                        subregions[i] = new List<Loc>
                        {
                            startPoint
                        };

                        visited[startPoint.X, startPoint.Y] = 2 + i;
                    }

                    foreach (Loc loc in island)
                    {
                        int closest = 0;
                        int minDist = Int32.MaxValue;
                        for (int i = 0; i < buildingCount; i++)
                        {
                            Loc sp = startPoints[i];
                            int dist = Distance.Manhatten(loc, sp);
                            if (dist < minDist)
                            {
                                closest = i;
                                minDist = dist;
                            }
                        }

                        visited[loc.X, loc.Y] = closest;
                        subregions[closest].Add(loc);

                        var tile = Map.Field[loc];
                        tile.Color = Colors.Blend(tile.Color, colorMap[closest], 0.8);
                    }

                    for (int i = 0; i < buildingCount; i++)
                    {
                        foreach (Loc loc in FindBoundary(subregions[i], i, visited))
                        {
                            Map.Field[loc].Terrain = TileType.Grass;
                            Map.Field[loc].Color = Color.DarkGreen.Blend(Color.White, Game.VisRand.NextDouble() * 0.5 + 0.1);
                        }
                    }
                }
            }
        }

        private IEnumerable<Loc> FindBoundary(List<Loc> region, int id, int[,] visited)
        {
            var boundary = new List<Loc>();

            foreach (Loc loc in region)
            {
                if ((loc.X > 0 && visited[loc.X - 1, loc.Y] != id)
                    || (loc.X < Width - 1 && visited[loc.X + 1, loc.Y] != id)
                    || (loc.Y > 0 && visited[loc.X, loc.Y - 1] != id)
                    || (loc.Y < Height - 1 && visited[loc.X, loc.Y + 1] != id))
                {
                    boundary.Add(loc);
                }
            }

            return boundary;
        }

        private IList<Loc> FindBoundary(List<Loc> region)
        {
            var boundary = new List<Loc>();

            foreach (Loc loc in region)
            {
                if ((loc.X > 0 && !Map.Field[loc.X - 1, loc.Y].IsWall)
                    || (loc.X < Width - 1 && !Map.Field[loc.X + 1, loc.Y].IsWall)
                    || (loc.Y > 0 && !Map.Field[loc.X, loc.Y - 1].IsWall)
                    || (loc.Y < Height - 1 && !Map.Field[loc.X, loc.Y + 1].IsWall))
                {
                    boundary.Add(loc);
                }
            }

            return boundary;
        }

        private IEnumerable<List<Loc>> FindIslands(int[,] visited)
        {
            var islands = new List<List<Loc>>();

            foreach (Tile tile in Map.Field)
            {
                if (visited[tile.X, tile.Y] != 0 || !tile.IsWall)
                    continue;

                var region = new List<Loc>();
                FloodFillRegion(tile.X, tile.Y, region, visited);
                islands.Add(region);
            }

            return islands;
        }

        private void FloodFillRegion(int x, int y, ICollection<Loc> region, int[,] visited)
        {
            visited[x, y] = 1;
            if (!Map.Field[x, y].IsWall || region.Count > 7000)
                return;

            region.Add(new Loc(x, y));
            if (x >= 1 && visited[x - 1, y] == 0)
            {
                FloodFillRegion(x - 1, y, region, visited);
            }
            if (y >= 1 && visited[x, y - 1] == 0)
            {
                FloodFillRegion(x, y - 1, region, visited);
            }
            if (x < Width - 1 && visited[x + 1, y] == 0)
            {
                FloodFillRegion(x + 1, y, region, visited);
            }
            if (y < Height - 1 && visited[x, y + 1] == 0)
            {
                FloodFillRegion(x, y + 1, region, visited);
            }
        }

        private void ProjectRoadToMap(Road road)
        {
            int startX = Math.Clamp((int)road.StartX, 0, Width - 1);
            int startY = Math.Clamp((int)road.StartY, 0, Height - 1);
            int endX = Math.Clamp((int)road.EndX, 0, Width - 1);
            int endY = Math.Clamp((int)road.EndY, 0, Height - 1);
            var startLoc = new Loc(startX, startY);
            var endLoc = new Loc(endX, endY);

            foreach (Loc loc in Map.GetStraightLinePath(startLoc, endLoc))
            {
                foreach (Loc s in Map.GetPointsInRadius(loc, road.Width, Measure.Manhatten))
                {
                    Map.Field[s].Terrain = TileType.Ground;
                }
            }
        }

        // TODO: use a population map to guide randomness
        private IEnumerable<Road> CreateRoads()
        {
            // Algorithm from http://nothings.org/gamedev/l_systems.html
            var potentialRoads = new SimplePriorityQueue<Road>();
            var acceptedRoads = new List<Road>();

            var StartX = Rand.Next(20, EngineConsts.MAP_WIDTH - 20);
            var StartY = Rand.Next(20, EngineConsts.MAP_HEIGHT - 20);
            var Length = Rand.NextDouble() * 15 + 10;
            var Angle = Math.PI / 2 * Rand.Next(0, 4);
            var r1 = new Road(0, StartX, StartY, Length, Angle, 5, 0, 0);
            var r2 = new Road(0, StartX, StartY, Length, Angle + Math.PI, 5, 0, 0);

            potentialRoads.Enqueue(r1, 0);
            potentialRoads.Enqueue(r2, 0);

            while (potentialRoads.Count > 0)
            {
                Road road = potentialRoads.First;
                float prio = potentialRoads.GetPriority(road);
                potentialRoads.Dequeue();

                if (CheckLocalConstraints(road, acceptedRoads, out Road newRoad))
                {
                    acceptedRoads.Add(newRoad);
                    ProjectRoadToMap(newRoad);

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

        private bool CheckLocalConstraints(Road road, List<Road> accepted, out Road newRoad)
        {
            newRoad = road;

            if (road.Generation >= 10)
                return false;

            if (newRoad.StartX < 0 || newRoad.StartX >= Width)
                return false;

            if (newRoad.StartY < 0 || newRoad.StartY >= Height)
                return false;


            foreach (Loc nearby in Map.GetPointsInRadius(new Loc((int)road.EndX, (int)road.EndY), 4, Measure.Chebyshev))
            {
                if (!Map.Field[nearby].IsWall)
                {
                    // hack, should extend the length to meet up with the road
                    newRoad.Length += 4;

                    if (Rand.NextDouble() < 0.9)
                        newRoad.Continue = false;

                    break;
                }
            }

            return true;
        }

        private IEnumerable<Road> SolveGlobalGoals(Road prevRoad)
        {
            // After every segment, we can continue straight, turn left, or turn right
            var newRoads = new Road[3];

            if (!prevRoad.Continue)
                return newRoads;

            newRoads[0] = new Road(
                prevRoad.Generation, prevRoad.EndX, prevRoad.EndY, Rand.NextDouble() * prevRoad.Width * 2 + 10,
                prevRoad.Angle + Rand.NextDouble() * Math.PI / 16 - Math.PI / 32, prevRoad.Width, prevRoad.LastSplitL + 1, prevRoad.LastSplitR + 1);

            const double baseSplitProb = 0.4;
            double splitProb = baseSplitProb + 0.2 * (double)prevRoad.LastSplitL;
            if (Rand.NextDouble() < splitProb)
            {
                newRoads[1] = GenerateBranch(prevRoad, -Math.PI / 2);
                newRoads[0].LastSplitL = 0;
            }

            splitProb = baseSplitProb + 0.2 * (double)prevRoad.LastSplitR;
            if (Rand.NextDouble() < splitProb)
            {
                newRoads[2] = GenerateBranch(prevRoad, Math.PI / 2);
                newRoads[0].LastSplitR = 0;
            }

            return newRoads;
        }

        private Road GenerateBranch(Road prevRoad, double offset)
        {
            int roadWidth;
            if (prevRoad.Generation == 0)
                roadWidth = prevRoad.Width - 1;
            else if (prevRoad.Width == 2)
                roadWidth = 2;
            else if (Rand.NextDouble() > 0.4)
                roadWidth = prevRoad.Width - 1;
            else
                roadWidth = prevRoad.Width;

            return new Road(
                prevRoad.Generation + 1, prevRoad.EndX, prevRoad.EndY, Rand.NextDouble() * roadWidth * 2 + 12,
                prevRoad.Angle + offset, roadWidth, 0, 0);
        }

        private class Road
        {
            public int Generation { get; }
            public double StartX { get; set; }
            public double StartY { get; set; }
            public double Length { get; set; }
            public double Angle { get; }
            public int Width { get; }
            public int LastSplitL { get; set; }
            public int LastSplitR { get; set; }
            public bool Continue { get; set; }

            public double EndX => StartX + Length * Math.Cos(Angle);
            public double EndY => StartY + Length * Math.Sin(Angle);

            public Road(int gen, double startX, double startY, double length, double angle, int width, int lastSplitL, int lastSplitR)
            {
                Generation = gen;
                StartX = startX;
                StartY = startY;
                Length = length;
                Angle = angle;
                Width = width;
                LastSplitL = lastSplitL;
                LastSplitR = lastSplitR;
                Continue = true;
            }
        }
    }
}
