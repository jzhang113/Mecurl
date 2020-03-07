using Engine;
using Mecurl.Actors;
using Mecurl.Parts;
using Optional;
using System.Drawing;

namespace Mecurl.Commands
{
    internal class MoveCommand : ICommand
    {
        public Mech Source { get; }
        public Option<IAnimation> Animation { get; private set; }

        private readonly Loc _nextPos;

        public MoveCommand(Mech source, in Loc pos)
        {
            Source = source;
            _nextPos = pos;
        }

        public Option<ICommand> Execute()
        {
            // Cancel out of bound moves.
            var bounds = Source.PartHandler.Bounds;
            var topleft = new Loc(_nextPos.X + bounds.Left, _nextPos.Y + bounds.Top);
            var botright = new Loc(_nextPos.X + bounds.Right - 1, _nextPos.Y + bounds.Bottom - 1);

            if (!Game.MapHandler.Field.IsValid(topleft) || !Game.MapHandler.Field.IsValid(botright))
            {
                Game.PrevCancelled = true;
                return Option.None<ICommand>();
            }

            // Don't walk into walls, unless the Actor is currently phasing or we are already
            // inside a wall (to prevent getting stuck).
            foreach (var p in Source.PartHandler)
            {
                for (int x = 0; x < p.Bounds.Width; x++)
                {
                    for (int y = 0; y < p.Bounds.Height; y++)
                    {
                        // we need to go from the bounding box locations to the actual pieces to
                        // check if they are passable
                        int boundsIndex = p.BoundingIndex(x, y);
                        if (p.IsPassable(boundsIndex)) continue;

                        int newX = x + p.Bounds.Left + _nextPos.X;
                        int newY = y + p.Bounds.Top + _nextPos.Y;
                        var tile = Game.MapHandler.Field[newX, newY];

                        if (tile.IsWall)
                        {
                            // Enemies naturally have WallWalk
                            if (Source == Game.Player && !Game.WallWalk)
                            {
                                // confirm walking through walls
                                Game.MessagePanel.Add("[color=warn]Alert[/color]: About to walk through wall");
                                Game.PrevCancelled = true;
                                Game.WallWalk = true;
                                return Option.None<ICommand>();
                            }

                            tile.IsWall = false;
                            tile.Symbol = CharUtils.GetRubbleSymbol();
                        }

                        // TODO: you can walk over other actors
                    }
                }
            }

            Game.MapHandler.SetActorPosition(Source, _nextPos);
            return Option.None<ICommand>();
        }
    }
}