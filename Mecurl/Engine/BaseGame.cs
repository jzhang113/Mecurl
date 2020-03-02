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
        internal TimeSpan FrameRate = new TimeSpan(TimeSpan.TicksPerSecond / 30);

        // HACK: how to communicate cancelled moved to the main loop?
        internal static bool PrevCancelled = false;
        private static bool _exiting;

        static BaseGame()
        {
            Rand = new Random();
            VisRand = new Random();
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
            IAnimation current = null;

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
                    if (current == null)
                    {
                        EventScheduler.ExecuteCommand(Player.Id, StateHandler.HandleInput(), () =>
                        {
                            if (!PrevCancelled)
                            {
                                MapHandler.Refresh();
                                EventScheduler.Update();
                            }
                            else
                            {
                                PrevCancelled = false;
                            }
                        });
                    }
                    else if (Terminal.HasInput())
                    {
                        Terminal.Read();
                    }

                    Ticks += FrameRate;
                    accum -= FrameRate;
                }

                double remaining = accum / FrameRate;

                AnimationHandler.Update(frameTime, remaining);
                Render();
            }

            Terminal.Close();
        }

        public abstract void Render();
    }
}
