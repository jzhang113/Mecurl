using Engine;
using Engine.Map;
using Mecurl.Actors;
using Optional;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Mecurl.Commands
{
    internal class AttackCommand : ICommand
    {
        public ISchedulable Source { get; }
        public Option<IAnimation> Animation { get; private set; }

        private readonly ICollection<Loc> _targets;
        private readonly double _power;

        public AttackCommand(ISchedulable source, int delay, double power, IEnumerable<Loc> targets, Option<IAnimation> animation)
        {
            Source = source;
            Animation = animation;
            _targets = targets.ToList();
            _power = power;
        }

        public Option<ICommand> Execute()
        {
            var potentialIntersects = new List<Mech>();

            // quick bounds check for each entity
            foreach (BaseActor unit in Game.MapHandler.Units.Values)
            {
                var actor = (Mech)unit;
                var bound = new Rectangle(actor.PartHandler.Bounds.Left + actor.Pos.X, actor.PartHandler.Bounds.Top + actor.Pos.Y, actor.PartHandler.Bounds.Width, actor.PartHandler.Bounds.Height);

                foreach (Loc loc in _targets)
                {
                    if (bound.Contains(loc.X, loc.Y))
                    {
                        potentialIntersects.Add(actor);
                        break;
                    };
                }
            }

            // assign damage to any entity identified by the bounds check
            foreach (Mech actor in potentialIntersects)
            {
                actor.AssignDamage(_targets, _power);
            }

            // assign damage to terrain
            foreach (Loc loc in _targets)
            {
                Tile tile = Game.MapHandler.Field[loc];
                if (tile.IsWall)
                {
                    tile.IsWall = false;

                    // TODO: formalize tile types + add score penalty for destroying buildings
                    tile.Symbol = CharUtils.GetRubbleSymbol();
                }
            }

            return Option.None<ICommand>();
        }
    }
}