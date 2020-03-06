using Engine.Drawing;
using Optional;
using System.Drawing;

namespace Engine
{
    public abstract class BaseActor : ISchedulable, IDrawable
    {
        private static int GlobalId = 0;
        public int Id { get; }

        public string Name { get; protected set; } = "Monster";
        public Color Color { get; protected set; }
        public char Symbol { get; }
        public bool ShouldDraw { get; set; }
        internal bool Moving { get; set; }

        public Loc Pos { get; set; }
        public bool BlocksLight { get; protected set; } = false;

        public int MaxHealth { get; }
        public int Health { get; set; }

        public int Speed { get; protected set; } = 100;

        protected BaseActor(in Loc pos, int hp, char symbol, Color color)
        {
            Pos = pos;
            MaxHealth = hp;
            Health = hp;

            Color = color;
            Symbol = symbol;
            ShouldDraw = true;

            Id = GlobalId++;
        }

        public abstract Option<ICommand> GetAction();

        public abstract Option<ICommand> TriggerDeath();

        public abstract bool DeathCheck();

        public Option<ICommand> Act()
        {
            if (DeathCheck())
                return TriggerDeath();
            else
                return GetAction();
        }

        public abstract void Draw(LayerInfo layer);
    }
}
