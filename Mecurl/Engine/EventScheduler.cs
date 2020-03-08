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

        public void Update()
        {
            bool done = false;
            while (!done && _schedule.Count > 0)
            {
                foreach ((ISchedulable entity, int value) in _schedule.ToList())
                {
                    int timeTilAct = value - entity.Speed;
                    if (timeTilAct <= 0)
                    {
                        if (entity.GetType() == _playerType)
                        {
                            Turn++;
                            done = true;
                        }
                        else
                        {
                            ExecuteCommand(entity, entity.Act(), () => { });

                            // remove any events that have completed
                            if (!(entity is BaseActor))
                            {
                                _schedule.Remove(entity);
                            }
                        }
                    }
                    else
                    {
                        _schedule[entity] = timeTilAct;
                    }
                }
            }
        }

        internal void ExecuteCommand(ISchedulable entity, Option<ICommand> action, Action after)
        {
            action.MatchSome(command =>
            {
                // note that Execute is allowed to modify TimeCost (which is necessary to detect
                // some things like wall walking)
                var retry = command.Execute();
                var animation = command.Animation;
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

                if (isEvent)
                {
                    Game.MapHandler.Refresh();

                    if (Game.Player.DeathCheck())
                    {
                        Game.Player.TriggerDeath();
                    }
                }

                after();
            });
        }
    }
}
