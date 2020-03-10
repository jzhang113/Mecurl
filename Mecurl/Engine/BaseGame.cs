using BearLib;
using Engine.Map;
using Optional;
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

        private const int updateLimit = 10;
        private static readonly TimeSpan TickRate = new TimeSpan(TimeSpan.TicksPerSecond / 60);

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
            TimeSpan maxDt = TickRate * updateLimit;

            bool playerTurn = false;
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

                while (accum >= TickRate)
                {
                    accum -= TickRate;

                    if (!playerTurn)
                    {
                        EventScheduler.FastForward();
                        playerTurn = EventScheduler.UpdateTick();

                        // eat any input if its not the player's turn to act
                        if (Terminal.HasInput())
                        {
                            Terminal.Read();
                        }

                        ProcessTurnEvents();
                    }
                    else
                    {
                        Option<ICommand> action = StateHandler.HandleInput();
                        EventScheduler.ExecuteCommand(Player, action);

                        action.MatchSome(_ =>
                        {
                            if (!PrevCancelled)
                            {
                                ProcessTurnEvents();
                                ProcessPlayerTurnEvents();
                                playerTurn = false;
                            }
                            else
                            {
                                PrevCancelled = false;
                            }
                        });
                    }
                }

                double remaining = accum / TickRate;
                AnimationHandler.Update(frameTime, remaining);
                Render();
            }

            Terminal.Close();
        }

        // general events that should run after any change to the world
        protected virtual void ProcessTurnEvents()
        {
            if (MapHandler != null)
            {
                MapHandler.Refresh();
            }

            if (Player != null && Player.DeathCheck())
            {
                Player.TriggerDeath();
            }
        }

        // events that run after the player has moved
        protected abstract void ProcessPlayerTurnEvents();

        public abstract void Render();
    }
}
