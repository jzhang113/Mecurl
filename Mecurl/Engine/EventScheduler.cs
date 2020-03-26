using Mecurl;
using Optional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class EventScheduler
    {
        internal static readonly IDictionary<ISchedulable, int> _schedule = new Dictionary<ISchedulable, int>();
        private static readonly IDictionary<Type, List<Action<ICommand>>> _subscribers;

        private readonly Type _playerType;

        public static int Turn { get; private set; }

        static EventScheduler()
        {
            _subscribers = new Dictionary<Type, List<Action<ICommand>>>();
            Type commandSupertype = typeof(ICommand);

            foreach (Type commandType in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => commandSupertype.IsAssignableFrom(p) && p.IsClass))
            {
                _subscribers.Add(commandType, new List<Action<ICommand>>());
            }
        }

        public EventScheduler(Type playerType)
        {
            _playerType = playerType;
            Turn = 0;
        }

        public void Clear()
        {
            _schedule.Clear();
        }

        internal void AddActor(BaseActor unit)
        {
            _schedule.Add(unit, unit.Speed);
        }

        internal void AddEvent(ISchedulable ev, int delay)
        {
            _schedule.Add(ev, delay);
        }

        internal void RemoveActor(BaseActor unit)
        {
            _schedule.Remove(unit);
        }

        public void Subscribe<T>(Action<ICommand> func) where T : ICommand
        {
            _subscribers[typeof(T)].Add(func);
        }

        // update a single tick, returns true if we should process player input
        public bool UpdateTick()
        {
            if (_schedule.Count == 0) return true;

            bool playerCanAct = false;
            foreach ((ISchedulable entity, int value) in _schedule.ToList())
            {
                int timeTilAct = value - entity.Speed;
                _schedule[entity] = timeTilAct;

                if (timeTilAct <= 0)
                {
                    if (entity.GetType() == _playerType)
                    {
                        Turn++;
                        playerCanAct = true;
                    }
                    else
                    {
                        ExecuteCommand(entity, entity.Act());

                        // remove any events that have completed
                        if (!(entity is BaseActor))
                        {
                            _schedule.Remove(entity);
                        }
                    }
                }
            }

            return playerCanAct;
        }

        // process events until something interesting is happening
        public void FastForward()
        {
            if (_schedule.Count == 0) return;

            var first = _schedule.First();
            int minTurnsTilAct = first.Value / first.Key.Speed;

            foreach ((ISchedulable entity, int ticks) in _schedule)
            {
                int turnsTilAct = ticks / entity.Speed;
                minTurnsTilAct = Math.Min(turnsTilAct, minTurnsTilAct);
            }

            foreach ((ISchedulable entity, int ticks) in _schedule.ToList())
            {
                _schedule[entity] = ticks - entity.Speed * minTurnsTilAct;
            }
        }

        internal void ExecuteCommand(ISchedulable entity, Option<ICommand> action)
        {
            if (entity == null) return;

            action.MatchSome(command =>
            {
                foreach (var handler in _subscribers[command.GetType()])
                {
                    handler.Invoke(command);
                }

                if (_schedule.ContainsKey(entity))
                {
                    _schedule[entity] += command.TimeCost;
                }
            });
        }
    }
}
