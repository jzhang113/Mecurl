using Engine.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class AnimationHandler
    {
        private readonly IDictionary<int, List<IAnimation>> _current;

        public AnimationHandler()
        {
            _current = new Dictionary<int, List<IAnimation>>();
        }

        public void Clear() => _current.Clear();

        public void Add(int id, IAnimation animation)
        {
            if (_current.TryGetValue(id, out List<IAnimation> queue))
                queue.Add(animation);
            else
                _current.Add(id, new List<IAnimation>() { animation });
        }

        public bool IsDone()
        {
            foreach ((int _, List<IAnimation> queue) in _current)
            {
                if (queue.Count != 0)
                    return false;
            }

            return true;
        }

        public void Update(TimeSpan frameTime, double remaining)
        {
            foreach ((int id, List<IAnimation> queue) in _current)
            {
                IAnimation animation = queue.FirstOrDefault();

                if (animation == null)
                    continue;

                // id -1 is for events, which doesn't respect turn cancelling
                // this is because the animation of events are instantiated before they occur
                // meaning that it looks like it is an old animation from several turns ago
                if (animation.Update(frameTime) || (id != -1 && EventScheduler.Turn > animation.Turn + 1))
                {
                    animation.Cleanup();
                    queue.Remove(animation);
                }
            }
        }

        public void Draw(LayerInfo layer)
        {
            foreach ((int _, List<IAnimation> queue) in _current)
            {
                IAnimation animation = queue.FirstOrDefault();

                if (animation != null)
                    animation.Draw(layer);
            }
        }
    }
}