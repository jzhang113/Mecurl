using Optional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class EventScheduler
    {
        private readonly IDictionary<ISchedulable, int> _schedule;
        private readonly Type _playerType;
        private readonly AnimationHandler _animationHandler;

        public static int Turn { get; private set; }

        public EventScheduler(Type playerType, AnimationHandler handler)
        {
            _playerType = playerType;
            _animationHandler = handler;
            _schedule = new Dictionary<ISchedulable, int>();
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
                    int timeTilAct = value - 1;
                    if (timeTilAct <= 0)
                    {
                        _schedule[entity] = entity.Speed;
                        if (entity.GetType() == _playerType)
                        {
                            Turn++;
                            done = true;
                        }
                        else
                        {
                            ExecuteCommand(entity.Id, entity.Act(), () => { });
                        }
                    }
                    else
                    {
                        _schedule[entity] = timeTilAct;
                    }
                }
            }
        }

        internal void ExecuteCommand(int sourceId, Option<ICommand> action, Action after)
        {
            action.MatchSome(command =>
            {
                var retry = command.Execute();
                var animation = command.Animation;

                while (retry.HasValue)
                {
                    retry.MatchSome(c =>
                    {
                        retry = c.Execute();
                        animation = c.Animation;
                    });
                }

                animation.MatchSome(anim => _animationHandler.Add(sourceId, anim));
                after();
            });
        }
    }
}
