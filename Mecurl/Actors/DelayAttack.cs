using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Optional;

namespace Mecurl
{
    internal class DelayAttack : ISchedulable
    {
        public int Id { get; }
        public string Name => " ";
        public int Speed => 4;

        private readonly int _delay;
        private readonly AttackCommand _attack;

        public DelayAttack(int delay, AttackCommand attack)
        {
            Id = BaseActor.GlobalId++;
            _delay = delay;
            _attack = attack;
        }

        public Option<ICommand> Act()
        {
            return Option.Some<ICommand>(_attack);
        }

        public void Draw(LayerInfo layer)
        {
            int ticksRemaining = EventScheduler._schedule[this];
            double fracProgress = (double)ticksRemaining / _delay;
            var color = Swatch.Compliment.Blend(Swatch.ComplimentLightest, fracProgress);

            Terminal.Color(color);
            Terminal.Composition(true);
            foreach (Loc loc in _attack.Targets)
            {
                layer.Put(loc.X - Camera.X, loc.Y - Camera.Y, '▓');
            }
            Terminal.Composition(false);
        }
    }
}
