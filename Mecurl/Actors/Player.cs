using Engine;
using Optional;
using System.Drawing;

namespace Mecurl.Actors
{
    public class Player : Actor
    {
        public Player(in Loc pos) : base(pos, 100, '@', Color.Wheat)
        {
            Name = "Player";
            Speed = 2;
        }

        // Commands processed in main loop
        public override Option<ICommand> GetAction()
        {
            return Option.None<ICommand>();
        }
    }
}
