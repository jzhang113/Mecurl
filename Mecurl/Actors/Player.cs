using Engine;
using Engine.Map;
using Mecurl.Parts;
using Optional;

namespace Mecurl.Actors
{
    class Player : Mech
    {
        public Player(in Loc pos, BaseMapHandler map, PartHandler partHandler) : base(pos, '@', Colors.Player, map, partHandler)
        {
            Name = "Player";
            _messages = Game.MessagePanel;
        }

        // Commands processed in main loop
        public override Option<ICommand> GetAction()
        {
            return Option.None<ICommand>();
        }

        public override Option<ICommand> TriggerDeath()
        {
            Game.GameOver();
            return Option.None<ICommand>();
        }
    }
}
