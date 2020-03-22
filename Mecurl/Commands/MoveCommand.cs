using Engine;
using Mecurl.Actors;
using Optional;
using System;

namespace Mecurl.Commands
{
    internal class MoveCommand : ICommand
    {
        public Mech Source { get; }
        public int TimeCost { get; private set; }

        public Option<IAnimation> Animation { get; private set; }

        private readonly Loc _nextPos;

        public MoveCommand(Mech source, in Loc pos)
        {
            Source = source;
            _nextPos = pos;
            TimeCost = source.PartHandler.GetMoveSpeed();
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

            bool wallPenalty = false;
            // walking through walls and other mechs
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
                                Game.MessagePanel.Add("[color=warn]Alert[/color]: Confirm walking through wall");
                                Game.PrevCancelled = true;
                                Game.WallWalk = true;
                                return Option.None<ICommand>();
                            }

                            tile.IsWall = false;
                            tile.Symbol = CharUtils.GetRubbleSymbol();

                            // wall walking is penalized by a speed reduction (but only apply this penalty once)
                            wallPenalty = true;
                        }

                        (char mechTile, _, int mechId) = Game.MapHandler.MechTileMap[newX, newY];
                        if (mechId != 0 && mechId != Source.Id && mechTile != ' ')
                        {
                            // don't walk over other mechs
                            if (Source == Game.Player)
                            {
                                Game.MessagePanel.Add("[color=warn]Alert[/color]: Cannot walk through another mech");
                                Game.PrevCancelled = true;
                            }

                            return Option.None<ICommand>();
                        }
                    }
                }
            }

            if (wallPenalty)
            {
                TimeCost *= 2;
            }

            Game.MapHandler.ForceSetMechPosition(Source, _nextPos);
            return Option.None<ICommand>();
        }
    }
}