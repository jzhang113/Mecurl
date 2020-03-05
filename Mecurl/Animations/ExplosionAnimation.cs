using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Mercurl.Animations
{
    internal class ExplosionAnimation : IAnimation
    {
        public int Turn { get; } = EventScheduler.Turn;
        public TimeSpan Duration { get; } = Game.FrameRate * 12;
        public TimeSpan CurrentTime { get; private set; }
        public TimeSpan EndTime { get; }

        private readonly IEnumerable<Loc> _pos;
        private readonly Color _color;
        private readonly Color _darkColor;
        private readonly Perlin _gen;

        public ExplosionAnimation(IEnumerable<Loc> pos, in Color color)
        {
            _pos = pos;
            _color = color;
            _darkColor = color.Blend(Color.Black, 0.6);

            CurrentTime = TimeSpan.Zero;
            EndTime = CurrentTime + Duration;

            _gen = new Perlin();
        }

        public bool Update(TimeSpan dt)
        {
            CurrentTime += dt;
            return CurrentTime >= EndTime;
        }

        public void Cleanup() { }

        public void Draw(LayerInfo layer)
        {
            double fracPassed = CurrentTime / Duration;
            Color between = _color.Blend(Colors.Floor, fracPassed);

            Terminal.Layer(3);
            foreach (Loc pos in _pos)
            {
                double d = _gen.OctavePerlin(pos.X, pos.Y, fracPassed * 10, 4, 0.5) * (1 - fracPassed * 0.4);
                Terminal.Color(between.Blend(_darkColor, d));

                char c;
                if (Game.VisRand.NextDouble() < fracPassed)
                {
                    c = ' ';
                }
                else if (Game.VisRand.NextDouble() < fracPassed * 2)
                {
                    c = '▓';
                }
                else
                {
                    c = '█';
                }

                layer.Put(pos.X - Camera.X, pos.Y - Camera.Y, c);
            }
            Terminal.Layer(layer.Z);
        }
    }
}
