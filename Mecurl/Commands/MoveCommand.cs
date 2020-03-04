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
            if (!Game.MapHandler.Field.IsValid(_nextPos))
                return Option.None<ICommand>();

            // Don't walk into walls, unless the Actor is currently phasing or we are already
            // inside a wall (to prevent getting stuck).

            Rectangle sourceBounds = Source.PartHandler.Bounds;
            Rectangle newBounds = new Rectangle(
                sourceBounds.X + _nextPos.X, sourceBounds.Y + _nextPos.Y,
                sourceBounds.Width, sourceBounds.Height);

            for (int x = newBounds.Left; x < newBounds.Right; x++)
            {
                for (int y = newBounds.Top; y < newBounds.Bottom; y++)
                {
                    if (Game.MapHandler.Field[x, y].IsWall)
                    {
                        // Don't penalize the player for walking into walls, but monsters should wait if 
                        // they will walk into a wall.
                        if (Source == Game.Player)
                            Game.PrevCancelled = true;

                        return Option.None<ICommand>();
                    }
                }
            }

            Game.MapHandler.SetActorPosition(Source, _nextPos);
            return Option.None<ICommand>();
        }
    }
}