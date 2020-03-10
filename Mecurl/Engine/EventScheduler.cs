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
        private readonly Type _playerType;
        private readonly AnimationHandler _animationHandler;

        public static int Turn { get; private set; }

        public EventScheduler(Type playerType, AnimationHandler handler)
        {
            _playerType = playerType;
            _animationHandler = handler;
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
            action.MatchSome(command =>
            {
                // note that Execute is allowed to modify TimeCost (which is necessary to detect
                // some things like wall walking)
                Option<ICommand> retry = command.Execute();
                Option<IAnimation> animation = command.Animation;
                int timeCost = command.TimeCost;

                while (retry.HasValue)
                {
                    retry.MatchSome(c =>
                    {
                        timeCost = c.TimeCost;
                        retry = c.Execute();
                        animation = c.Animation;
                    });
                }

                _schedule[entity] += timeCost;

                bool isEvent = !(entity is BaseActor);
                // events get a special id of -1 for animations
                int animId = isEvent ? -1 : entity.Id;
                animation.MatchSome(anim => _animationHandler.Add(animId, anim));
            });
        }
    }
}
