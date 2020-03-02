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

            var roads = CreateRoads(noiseMap);

            CreateRoom(new Room(1, 1, Width - 2, Height - 2));
        }

        protected override void PlaceActors()
        {
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
            potentialRoads.Enqueue(new Road(Rand), 0);

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
                        if (rq.Lifetime >= 0)
                        {
                            potentialRoads.Enqueue(rq, prio + 1 + rq.Lifetime);
                        }
                    }
                }
            }

            return acceptedRoads;
        }

        private bool CheckLocalConstraints(Road road, out Road newRoad)
        {
            newRoad = road;
            return true;
        }

        private IEnumerable<Road> SolveGlobalGoals(Road prevRoad)
        {
            return new Road[0];
        }

        private class Road
        {
            public int Lifetime { get; set; }
            public Loc Start { get; set; }
            public Loc End { get; set; }

            public Road(Random rand)
            {
                Lifetime = 10;
                Start = new Loc(rand.Next(0, 100), rand.Next(0, 100));
                End = new Loc(rand.Next(0, 100), rand.Next(0, 100));
            }
        }
    }
}
