using Engine;

namespace Mecurl.Commands
{
    internal class DelayAttackCommand : ICommand
    {
        public int TimeCost { get; }
        public int Delay { get; }
        public AttackCommand Attack { get; }

        public DelayAttackCommand(int delay, AttackCommand attack)
        {
            Delay = delay;
            Attack = attack;
            TimeCost = attack.TimeCost;
        }
    }
}
