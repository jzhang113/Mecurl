using Engine;
using Mecurl.Actors;
using Optional;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.Commands
{
    internal class AttackCommand : ICommand
    {
        public ISchedulable Source { get; }
        public Option<IAnimation> Animation { get; private set; }

        private readonly IEnumerable<Loc> _targets;
        private readonly double _power;

        public AttackCommand(ISchedulable source, int delay, double power, IEnumerable<Loc> targets)
        {
            Source = source;
            _targets = targets;
            _power = power;
        }

        public Option<ICommand> Execute()
        {
            var potentialIntersects = new List<Actor>();

            // quick bounds check for each entity
            foreach (BaseActor unit in Game.MapHandler.Units.Values)
            {
                var actor = (Actor)unit;
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
            foreach (Actor actor in potentialIntersects)
            {
                actor.PartHandler.AssignDamage(_targets, _power);
            }

            // assign damage to terrain
            foreach (Loc loc in _targets)
            {
                Engine.Map.Tile tile = Game.MapHandler.Field[loc];
                if (tile.IsWall)
                {
                    tile.IsWall = false;

                    // TODO: formalize tile types + add score penalty for destroying buildings
                    double rubble = Game.VisRand.NextDouble();
                    if (rubble < 0.3)
                    {
                        tile.Symbol = ';';
                    }
                    else if (rubble < 0.6)
                    {
                        tile.Symbol = ',';
                    }
                    else
                    {
                        tile.Symbol = '.';
                    }
                }
            }

            return Option.None<ICommand>();
        }
    }
}