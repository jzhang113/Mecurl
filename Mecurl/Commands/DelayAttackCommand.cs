using Engine;
using Optional;

namespace Mecurl.Commands
{
    internal class DelayAttackCommand : ICommand
    {
        public Option<IAnimation> Animation => Option.None<IAnimation>();
        public int TimeCost { get; }

        private readonly int _delay;
        private readonly AttackCommand _attack;

        public DelayAttackCommand(int delay, AttackCommand attack)
        {
            _delay = delay;
            _attack = attack;

            TimeCost = attack.TimeCost;
        }

        public Option<ICommand> Execute()
        {
            Game.EventScheduler.AddEvent(new DelayAttack(_delay, _attack), _delay);
            return Option.None<ICommand>();
        }
    }
}
