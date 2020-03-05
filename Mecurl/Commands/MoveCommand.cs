using Engine;
using Mecurl.Actors;
using Optional;
using System.Drawing;

namespace Mecurl.Commands
{
    internal class MoveCommand : ICommand
    {
        public Actor Source { get; }
        public Option<IAnimation> Animation { get; private set; }

        private readonly Loc _nextPos;

        public MoveCommand(Actor source, in Loc pos)
        {
            Source = source;
            _nextPos = pos;
        }

        public Option<ICommand> Execute()
        {
            // Cancel out of bound moves.
            var bounds = Source.PartHandler.Bounds;
            var topleft = new Loc(_nextPos.X + bounds.Left, _nextPos.Y + bounds.Top);
            var botright = new Loc(_nextPos.X + bounds.Bottom - 2, _nextPos.Y + bounds.Right - 2);

            if (!Game.MapHandler.Field.IsValid(topleft) || !Game.MapHandler.Field.IsValid(botright))
                return Option.None<ICommand>();

            // Don't walk into walls, unless the Actor is currently phasing or we are already
            // inside a wall (to prevent getting stuck).
            foreach (var p in Source.PartHandler)
            {
                for (int x = 0; x < p.Bounds.Width; x++)
                {
                    for (int y = 0; y < p.Bounds.Height; y++)
                    {
                        // here we are looping over the *bounding box*
                        // we need to go from the bounding box locations to the actual pieces to
                        // check if they are passable

                        // note that if the facing is N/S, then p.Bounds.Width == p.Width, so the
                        // boundsIndex correspond to the indices of p.Structure before adjustment
                        // however, if the facing is W/E, then the dimensions are flipped, so we
                        // need to compute the correct corresponding index of p.Structure and then
                        // adjust
                        int boundsIndex = -1;
                        if (p.Facing == Direction.N || p.Facing == Direction.S)
                        {
                            boundsIndex = x + y * p.Width;
                        }
                        else if (p.Facing == Direction.W || p.Facing == Direction.E)
                        {
                            boundsIndex = x * p.Width + y;
                        }

                        if (p.IsPassable(boundsIndex))
                        {
                            continue;
                        }
                        
                        int newX = x + p.Bounds.Left + _nextPos.X;
                        int newY = y + p.Bounds.Top + _nextPos.Y;

                        if (Game.MapHandler.Field[newX, newY].IsWall)
                        {
                            // Don't penalize the player for walking into walls, but monsters should wait if 
                            // they will walk into a wall.
                            if (Source == Game.Player)
                                Game.PrevCancelled = true;

                            return Option.None<ICommand>();
                        }
                    }
                }
            }

            Game.MapHandler.SetActorPosition(Source, _nextPos);
            return Option.None<ICommand>();
        }
    }
}