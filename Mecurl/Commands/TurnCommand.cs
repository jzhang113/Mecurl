using Engine;
using Mecurl.Actors;
using Optional;
using System;
using System.Drawing;

namespace Mecurl.Commands
{
    internal class TurnCommand : ICommand
    {
        public Mech Source { get; }
        public int TimeCost { get; }
        public Option<IAnimation> Animation => Option.None<IAnimation>();

        // we only actually care about two specific rotations, 90 to the left and 90 to the right
        private readonly bool _rightTurn;

        public TurnCommand(Mech source, double angle)
        {
            Source = source;

            // turning is twice as fast as moving
            TimeCost = source.PartHandler.GetMoveSpeed() / 2;

            // we use the right-hand rule, so negative rotations are clockwise
            _rightTurn = angle < 0;
        }

        public Option<ICommand> Execute()
        {
            var map = Game.MapHandler;

            // cancel out of bound moves.
            Rectangle bounds = Source.PartHandler.Bounds;
            Rectangle rotatedBounds;

            if (_rightTurn)
            {
                rotatedBounds = Rectangle.FromLTRB(-bounds.Bottom + 1, bounds.Left, -bounds.Top + 1, bounds.Right);
            }
            else
            {
                rotatedBounds = Rectangle.FromLTRB(bounds.Top, -bounds.Right + 1, bounds.Bottom, -bounds.Left + 1);
            }

            var topleft = new Loc(Source.Pos.X + rotatedBounds.Left, Source.Pos.Y + rotatedBounds.Top);
            var botright = new Loc(Source.Pos.X + rotatedBounds.Right - 1, Source.Pos.Y + rotatedBounds.Bottom - 1);

            if (!Game.MapHandler.Field.IsValid(topleft) || !Game.MapHandler.Field.IsValid(botright))
            {
                Game.MessagePanel.Add("[color=warn]Alert[/color]: No space to turn");
                Game.PrevCancelled = true;
                return Option.None<ICommand>();
            }

            if (_rightTurn)
            {
                Source.RotateRight();
            }
            else
            {
                Source.RotateLeft();
            }

            // at the point, we could cancel turns if they would go into buildings, but imo its
            // not fun to have to determine where a rotation is feasible
            // instead, rotations will always beat out and destroy buildings and push out enemies
            foreach (var p in Source.PartHandler)
            {
                for (int x = 0; x < p.Bounds.Width; x++)
                {
                    for (int y = 0; y < p.Bounds.Height; y++)
                    {
                        int boundsIndex = p.BoundingIndex(x, y);
                        if (p.IsPassable(boundsIndex)) continue;

                        int newX = Source.Pos.X + x + p.Bounds.Left;
                        int newY = Source.Pos.Y + y + p.Bounds.Top;
                        var tile = map.Field[newX, newY];

                        if (tile.IsWall)
                        {
                            tile.IsWall = false;
                            tile.Symbol = CharUtils.GetRubbleSymbol();
                        }

                        (char mechTile, _, int mechId) = Game.MapHandler.MechTileMap[newX, newY];
                        if (mechId != 0 && mechId != Source.Id && mechTile != ' ')
                        {
                            // don't walk over other mechs
                            if (Source == Game.Player)
                            {
                                Game.MessagePanel.Add("[color=warn]Alert[/color]: Cannot turn through another mech");
                                Game.PrevCancelled = true;
                            }

                            return Option.None<ICommand>();
                        }
                    }
                }
            }

            return Option.None<ICommand>();
        }
    }
}
