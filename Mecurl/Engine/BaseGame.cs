using BearLib;
using Engine.Map;
using System;

namespace Engine
{
    public abstract class BaseGame
    {
        public static MapHandler MapHandler { get; protected set; }
        public static StateHandler StateHandler { get; protected set; }
        public static EventScheduler EventScheduler { get; protected set; }
        public static AnimationHandler AnimationHandler { get; protected set; }
        public static BaseActor Player { get; protected set; }

        public static Random Rand { get; }
        public static Random VisRand { get; }

        internal TimeSpan Ticks;
        internal static TimeSpan FrameRate = new TimeSpan(TimeSpan.TicksPerSecond / 30);

        // HACK: how to communicate cancelled moved to the main loop?
        internal static bool PrevCancelled = false;
        internal static bool WallWalk = false;
        private static bool _exiting;

        static BaseGame()
        {
            Rand = new Random();
            int s1 = Rand.Next();
            int s2 = Rand.Next();
            Console.WriteLine($"seed: {s1}, {s2}");

            Rand = new Random(s1);
            VisRand = new Random(s2);
        }

        public static void Exit()
        {
            _exiting = true;
        }

        public void Run()
        {
            DateTime currentTime = DateTime.UtcNow;
            var accum = new TimeSpan();

            const int updateLimit = 10;
            TimeSpan maxDt = FrameRate * updateLimit;

            while (!_exiting)
            {
                DateTime newTime = DateTime.UtcNow;
                TimeSpan frameTime = newTime - currentTime;
                if (frameTime > maxDt)
                {
                    frameTime = maxDt;
                }

                currentTime = newTime;
                accum += frameTime;

                while (accum >= FrameRate)
                {
                    EventScheduler.ExecuteCommand(Player, StateHandler.HandleInput(), () =>
                    {
                        if (!PrevCancelled)
                        {
                            ProcessTickEvents();
                            MapHandler.Refresh();
                            EventScheduler.Update();
                        }
                        else
                        {
                            PrevCancelled = false;
                        }
                    });

                    Ticks += FrameRate;
                    accum -= FrameRate;
                }

                double remaining = accum / FrameRate;

                AnimationHandler.Update(frameTime, remaining);
                Render();
            }

            Terminal.Close();
        }

        // Any events that should happen once per player turn (such as checking for player death)
        protected abstract void ProcessTickEvents();

        public abstract void Render();
    }
}
