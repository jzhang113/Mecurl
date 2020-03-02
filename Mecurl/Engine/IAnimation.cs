using Engine.Drawing;
using System;

namespace Engine
{
    public interface IAnimation
    {
        int Turn { get; }

        TimeSpan Duration { get; }

        TimeSpan CurrentTime { get; }

        TimeSpan EndTime { get; }

        // Returns true when an animation is done updating
        bool Update(TimeSpan dt);

        // Draw the animation to Layer
        void Draw(LayerInfo layer);

        // Any cleanup that needs to be done, like unhiding Actors
        void Cleanup();
    }
}
