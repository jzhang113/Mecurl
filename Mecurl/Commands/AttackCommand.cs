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
        public int TimeCost { get; }
        public Option<IAnimation> Animation { get; private set; }

        public ICollection<Loc> Targets { get; }

        private readonly double _power;

        // HACK: cheating so lasers and beams don't hit yourself
        private readonly bool _ignoreselfdamage;

        public AttackCommand(ISchedulable source, int timeCost, double power, IEnumerable<Loc> targets, Option<IAnimation> animation, bool ignoreselfdamage = false)
        {
            Source = source;
            TimeCost = timeCost;
            Animation = animation;
            Targets = targets.ToList();
            _power = power;
            _ignoreselfdamage = ignoreselfdamage;
        }

        public Option<ICommand> Execute()
        {
            var potentialIntersects = new List<Mech>();

            // quick bounds check for each entity
            foreach (BaseActor unit in Game.MapHandler.Units.Values)
            {
                var actor = (Mech)unit;
                var bound = new Rectangle(actor.PartHandler.Bounds.Left + actor.Pos.X, actor.PartHandler.Bounds.Top + actor.Pos.Y, actor.PartHandler.Bounds.Width, actor.PartHandler.Bounds.Height);

                if (_ignoreselfdamage && unit == Source)
                    continue;

                foreach (Loc loc in Targets)
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
                actor.AssignDamage(Targets, _power);
            }

            // assign damage to terrain
            foreach (Loc loc in Targets)
            {
                Tile tile = Game.MapHandler.Field[loc];
                if (tile.IsWall)
                {
                    tile.Terrain = TileType.Debris;
                }
            }

            return Option.None<ICommand>();
        }
    }
}