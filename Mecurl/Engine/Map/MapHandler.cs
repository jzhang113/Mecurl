using BearLib;
using Engine.Drawing;
using Optional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Map
{
    public class MapHandler
    {
        public Option<Loc> Exit { get; internal set; }
        public int Width { get; }
        public int Height { get; }
        public int Level { get; }

        internal Field Field { get; }
        internal int[,] Clearance { get; }

        // internal transient helper structures
        internal int[,] PlayerMap { get; }
        internal IDictionary<int, BaseActor> Units { get; }
        internal IDictionary<int, BaseItem> Items { get; }

        // keep queue to prevent unnecessary allocations
        private readonly Queue<LocCost> _goals = new Queue<LocCost>();

        private readonly Measure _measure;

        public MapHandler(int width, int height, int level)
        {
            Width = width;
            Height = height;
            Level = level;
            Exit = Option.None<Loc>();

            Field = new Field(width, height);
            Clearance = new int[width, height];
            PlayerMap = new int[width, height];
            Units = new Dictionary<int, BaseActor>();
            Items = new Dictionary<int, BaseItem>();

            _measure = EngineConsts.MEASURE;
        }

        // Recalculate the state of the world after movements happen. If only light recalculations
        // needed, call UpdatePlayerFov() instead.
        internal void Refresh()
        {
            Camera.UpdateCamera(BaseGame.Player.Pos);
            UpdatePlayerFov();
            UpdatePlayerMaps();
        }

        #region Actor Methods
        public bool AddActor(BaseActor unit)
        {
            if (!Field[unit.Pos].IsWalkable)
                return false;

            SetActorPosition(unit, unit.Pos);
            BaseGame.EventScheduler.AddActor(unit);
            return true;
        }

        public Option<BaseActor> GetActor(in Loc pos) =>
            Units.TryGetValue(ToIndex(pos), out BaseActor actor) ? Option.Some(actor) : Option.None<BaseActor>();

        public bool RemoveActor(BaseActor unit)
        {
            if (!Units.Remove(ToIndex(unit.Pos)))
                return false;

            Tile unitTile = Field[unit.Pos];
            unitTile.IsOccupied = false;
            unitTile.BlocksLight = false;

            // unit.State = ActorState.Dead;
            BaseGame.EventScheduler.RemoveActor(unit);
            return true;
        }

        public bool SetActorPosition(BaseActor actor, in Loc pos)
        {
            if (!Field[pos].IsWalkable)
                return false;

            Tile tile = Field[actor.Pos];
            Tile newTile = Field[pos];

            tile.IsOccupied = false;
            tile.BlocksLight = false;
            Units.Remove(ToIndex(actor.Pos));

            actor.Pos = pos;
            newTile.IsOccupied = true;
            newTile.BlocksLight = actor.BlocksLight;
            Units.Add(ToIndex(pos), actor);

            return true;
        }

        // Calculate walkability, based on Actor size and status
        public bool IsWalkable(BaseActor actor, in Loc pos)
        {
            // check phasing
            //if (actor.StatusHandler.TryGetStatus(Statuses.StatusType.Phasing, out _))
            //    return true;

            // check clearance
            //if (actor.Size > Clearance[pos.X, pos.Y])
            //    return false;

            // check all spaces are unoccupied
            foreach (Loc point in GetPointsInRect(pos, 1, 1))
            {
                if (Field[point].IsOccupied)
                    return false;
            }

            return true;
        }
        #endregion

        #region Item Methods
        public void AddItem(BaseItem item)
        {
            int index = ToIndex(item.Pos);
            if (Items.ContainsKey(index))
            {
                // one item per tile, excess spill to nearby, up to radius 3
                const int maxSpillRadius = 3;
                bool done = false;

                for (int r = 1; r <= maxSpillRadius; r++)
                {
                    if (done) break;

                    GetPointsInRadiusBorder(item.Pos, r)
                        .Where(check => Items.ContainsKey(ToIndex(check)))
                        .Random(BaseGame.Rand)
                        .MatchSome(open =>
                        {
                            Items.Add(ToIndex(open), item);
                            done = true;
                        });
                }
            }
            else
            {
                Items.Add(index, item);
            }
        }

        public Option<BaseItem> GetItem(in Loc pos)
        {
            bool found = Items.TryGetValue(ToIndex(pos), out BaseItem item);
            return found ? Option.Some(item) : Option.None<BaseItem>();
        }

        public bool RemoveItem(BaseItem item)
        {
            int index = ToIndex(item.Pos);
            if (!Items.ContainsKey(index))
                return false;

            Items.Remove(index);
            return true;
        }
        #endregion

        #region Tile Selection Methods
        public Loc GetRandomOpenPoint()
        {
            int xPos;
            int yPos;
            do
            {
                xPos = BaseGame.Rand.Next(1, Width - 1);
                yPos = BaseGame.Rand.Next(1, Height - 1);
            } while (!Field[xPos, yPos].IsWalkable);

            return new Loc(xPos, yPos);
        }

        public Option<Loc> GetRandomOpenPoint(int minClearance, int maxTries)
        {
            for (int i = 0; i < maxTries; i++)
            {
                int xPos = BaseGame.Rand.Next(1, Width - 1);
                int yPos = BaseGame.Rand.Next(1, Height - 1);

                if (Clearance[xPos, yPos] >= minClearance)
                {
                    return Option.Some(new Loc(xPos, yPos));
                }
            }

            return Option.None<Loc>();
        }

        public IEnumerable<LocCost> GetPathToPlayer(Loc pos)
        {
            System.Diagnostics.Debug.Assert(Field.IsValid(pos));
            int nearest = PlayerMap[pos.X, pos.Y];
            int prev = nearest;

            while (nearest > 0)
            {
                LocCost nextMove = MoveTowardsTarget(pos, PlayerMap, _measure);
                nearest = nextMove.Cost;

                if (Math.Abs(nearest - prev) < 0.001f || Math.Abs(nearest) < 0.01f)
                {
                    yield break;
                }
                else
                {
                    prev = nearest;
                    yield return nextMove;
                }
            }
        }

        internal LocCost MoveTowardsTarget(in Loc current, int[,] goalMap, Measure m, bool openDoors = false)
        {
            Loc next = current;
            int nearest = goalMap[current.X, current.Y];

            foreach (Loc newPos in GetPointsInRadius(current, 1, m))
            {
                if (goalMap[newPos.X, newPos.Y] != -1 && goalMap[newPos.X, newPos.Y] < nearest)
                {
                    //if (Field[newX, newY].IsWalkable || Math.Abs(goalMap[newX, newY]) < 0.001f)
                    {
                        next = newPos;
                        nearest = goalMap[newPos.X, newPos.Y];
                    }
                    //else if (openDoors && TryGetDoor(newX, newY, out _))
                    //{
                    //    nextX = newX;
                    //    nextY = newY;
                    //    nearest = goalMap[newX, newY];
                    //}
                }
            }

            return new LocCost(next, nearest);
        }

        public IEnumerable<Loc> GetStraightPathToPlayer(in Loc pos)
        {
            System.Diagnostics.Debug.Assert(Field.IsValid(pos));
            return Field[pos].IsVisible
                ? GetStraightLinePath(pos, BaseGame.Player.Pos)
                : new List<Loc>();
        }

        // Returns a straight line from the source to target. Does not check if the path is actually
        // walkable.
        public IEnumerable<Loc> GetStraightLinePath(Loc source, Loc target)
        {
            int dx = Math.Abs(target.X - source.X);
            int dy = Math.Abs(target.Y - source.Y);
            int sx = target.X < source.X ? -1 : 1;
            int sy = target.Y < source.Y ? -1 : 1;
            int err = dx - dy;

            // Skip initial position?
            // yield return Field[sourceX, sourceY];

            // Take a step towards the target and return the new position.
            int xCurr = source.X;
            int xEnd = target.X;
            int yCurr = source.Y;
            int yEnd = target.Y;

            while (xCurr != xEnd || yCurr != yEnd)
            {
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    xCurr += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    yCurr += sy;
                }

                yield return new Loc(xCurr, yCurr);
            }
        }

        public IEnumerable<Loc> GetPointsInRadius(Loc origin, int radius, Measure m)
        {
            switch (m) {
                case Measure.Chebyshev:
                    for (int i = origin.X - radius; i <= origin.X + radius; i++)
                    {
                        for (int j = origin.Y - radius; j <= origin.Y + radius; j++)
                        {
                            if (Field.IsValid(i, j))
                                yield return new Loc(i, j);
                        }
                    }
                    break;
                case Measure.Manhatten:
                    for (int i = origin.X - radius; i <= origin.X + radius; i++)
                    {
                        for (int j = origin.Y - radius; j <= origin.Y + radius; j++)
                        {
                            if (Field.IsValid(i, j) && Distance.Manhatten(new Loc(i, j), origin) <= radius)
                                yield return new Loc(i, j);
                        }
                    }
                    break;
                case Measure.Euclidean:
                    for (int i = origin.X - radius; i <= origin.X + radius; i++)
                    {
                        for (int j = origin.Y - radius; j <= origin.Y + radius; j++)
                        {
                            if (Field.IsValid(i, j) && Distance.EuclideanSquared(new Loc(i, j), origin) <= radius * radius)
                                yield return new Loc(i, j);
                        }
                    }
                    break;
            }
        }

        public IEnumerable<Loc> GetPointsInRadiusBorder(Loc origin, int radius)
        {
            for (int i = origin.X - radius + 1; i < origin.X + radius; i++)
            {
                if (Field.IsValid(i, origin.Y - radius))
                    yield return new Loc(i, origin.Y - radius);

                if (Field.IsValid(i, origin.Y + radius))
                    yield return new Loc(i, origin.Y + radius);
            }

            for (int j = origin.Y - radius; j <= origin.Y + radius; j++)
            {
                if (Field.IsValid(origin.X - radius, j))
                    yield return new Loc(origin.X - radius, j);

                if (Field.IsValid(origin.X + radius, j))
                    yield return new Loc(origin.X + radius, j);
            }
        }

        public IEnumerable<Loc> GetPointsInRect(Loc origin, int width, int height)
        {
            for (int i = origin.X; i < origin.X + width; i++)
            {
                for (int j = origin.Y; j < origin.Y + height; j++)
                {
                    if (Field.IsValid(i, j))
                        yield return new Loc(i, j);
                }
            }
        }

        public IEnumerable<Loc> GetPointsInRectBorder(Loc origin, int width, int height)
        {
            for (int i = origin.X + 1; i < origin.X + width; i++)
            {
                if (Field.IsValid(i, origin.Y))
                    yield return new Loc(i, origin.Y);

                if (Field.IsValid(i, origin.Y + height))
                    yield return new Loc(i, origin.Y + height);
            }

            for (int j = origin.Y; j <= origin.Y + height; j++)
            {
                if (Field.IsValid(origin.X, j))
                    yield return new Loc(origin.X, j);

                if (Field.IsValid(origin.X + width, j))
                    yield return new Loc(origin.X + width, j);
            }
        }

        // Octants are identified by the direction of the right edge. Returns a row starting from
        // the straight edge.
        private IEnumerable<Tile> GetRowInOctant(int x, int y, int distance, Direction dir)
        {
            if (dir == Direction.Center)
                yield break;

            for (int i = 0; i <= distance; i++)
            {
                (int dx, int dy) = dir switch
                {
                    Direction.N => (-i, -distance),
                    Direction.NW => (-distance, -i),
                    Direction.W => (-distance, i),
                    Direction.SW => (-i, distance),
                    Direction.S => (i, distance),
                    Direction.SE => (distance, i),
                    Direction.E => (distance, -i),
                    Direction.NE => (i, -distance),
                    _ => (0, 0),
                };

                if (Field.IsValid(x + dx, y + dy))
                    yield return Field[x + dx, y + dy];
                else
                    yield break;
            }
        }
        #endregion

        #region FOV Methods
        public void ComputeFov(in Loc pos, double lightDecay, bool setVisible)
        {
            foreach (Direction dir in DirectionExtensions.DirectionList)
            {
                var visibleRange = new Queue<AngleRange>();
                visibleRange.Enqueue(new AngleRange(1, 0, 1, 1));

                while (visibleRange.Count > 0)
                {
                    AngleRange range = visibleRange.Dequeue();
                    // If we don't care about setting visibility, we can stop once lightLevel reaches
                    // 0. Otherwise, we need to continue to check if los exists.
                    if (!setVisible && range.LightLevel < EngineConsts.MIN_VISIBLE_LIGHT_LEVEL)
                        continue;

                    // There is really no need to check past 100 or something.
                    // TODO: put safeguards for when map gen borks and excavates the edges of the map
                    if (range.Distance > 100)
                        continue;

                    double delta = 0.5 / range.Distance;
                    IEnumerable<Tile> row = GetRowInOctant(pos.X, pos.Y, range.Distance, dir);

                    CheckFovInRange(range, row, delta, visibleRange, lightDecay, setVisible);
                }
            }
        }

        // Sweep across a row and update the set of unblocked angles for the next row.
        private static void CheckFovInRange(in AngleRange range, IEnumerable<Tile> row, double delta,
            Queue<AngleRange> queue, double lightDecay, bool setVisible)
        {
            double currentAngle = 0;
            double newMinAngle = range.MinAngle;
            double newMaxAngle = range.MaxAngle;
            bool prevLit = false;
            bool first = true;

            foreach (Tile tile in row)
            {
                if (currentAngle > range.MaxAngle && Math.Abs(currentAngle - range.MaxAngle) > 0.001)
                {
                    // The line to the current tile falls outside the maximum angle. Partially
                    // light the tile and lower the maximum angle if we hit a wall.
                    double visiblePercent = (range.MaxAngle - currentAngle) / (2 * delta) + 0.5;
                    if (visiblePercent > 0)
                        tile.Light += (float)(visiblePercent * range.LightLevel);

                    if (setVisible)
                        tile.LosExists = true;

                    if (!tile.IsLightable)
                        newMaxAngle = currentAngle - delta;
                    break;
                }

                if (currentAngle > range.MinAngle || Math.Abs(currentAngle - range.MinAngle) < 0.001)
                {
                    double beginAngle = currentAngle - delta;
                    double endAngle = currentAngle + delta;

                    // Set the light level to the percent of tile visible. Note that tiles in a
                    // straight line from the center have their light values halved as each octant
                    // only covers half of the cells on the edges.
                    if (endAngle > range.MaxAngle)
                    {
                        double visiblePercent = (range.MaxAngle - currentAngle) / (2 * delta) + 0.5;
                        tile.Light += (float)(visiblePercent * range.LightLevel);
                    }
                    else if (beginAngle < range.MinAngle)
                    {
                        double visiblePercent = (currentAngle - range.MinAngle) / (2 * delta) + 0.5;
                        tile.Light += (float)(visiblePercent * range.LightLevel);
                    }
                    else
                    {
                        tile.Light += (float)range.LightLevel;
                    }

                    if (setVisible)
                    {
                        tile.LosExists = true;

                        // Since this method calculates LOS at the same time as player lighting,
                        // only set what the player can see as explored.
                        if (tile.Light > EngineConsts.MIN_VISIBLE_LIGHT_LEVEL)
                            tile.IsExplored = true;
                    }

                    // For the first tile in a row, we only need to consider whether the current
                    // tile is blocked or not.
                    if (first)
                    {
                        first = false;
                        newMinAngle = !tile.IsLightable ? endAngle : range.MinAngle;
                    }
                    else
                    {
                        // If we are transitioning from an unblocked tile to a blocked tile, we need
                        // to lower the maximum angle for the next row.
                        if (prevLit && !tile.IsLightable)
                        {
                            int newDist = range.Distance + 1;
                            double light = range.LightLevel * (1 - lightDecay) * (1 - lightDecay);
                            queue.Enqueue(new AngleRange(newDist, newMinAngle, beginAngle, light));

                            // Update the minAngle to deal with single width walls in hallways.
                            newMinAngle = endAngle;
                        }
                        else if (!tile.IsLightable)
                        {
                            // If we are transitioning from a blocked tile to an unblocked tile, we
                            // need to raise the minimum angle.
                            newMinAngle = endAngle;
                        }
                    }
                }

                prevLit = tile.IsLightable;
                currentAngle += 2 * delta;
            }

            if (prevLit)
            {
                int newDist = range.Distance + 1;
                double light = range.LightLevel * (1 - lightDecay) * (1 - lightDecay);
                queue.Enqueue(new AngleRange(newDist, newMinAngle, newMaxAngle, light));
            }
        }

        private readonly struct AngleRange
        {
            internal int Distance { get; }
            internal double MinAngle { get; }
            internal double MaxAngle { get; }
            internal double LightLevel { get; }

            public AngleRange(int distance, double minAngle, double maxAngle, double lightLevel)
            {
                Distance = distance;
                MinAngle = minAngle;
                MaxAngle = maxAngle;
                LightLevel = lightLevel;
            }
        }
        #endregion

        #region Drawing Methods
        public void Draw(LayerInfo layer)
        {
            // draw borders
            Terminal.Color(Colors.BorderColor);
            layer.DrawBorders(new BorderInfo
            {
                TopLeftChar = '┬',
                TopRightChar = '┐',
                TopChar = '─',
                LeftChar = '│', // 179
                RightChar = '│'
            });

            // draw everything else
            Terminal.Color(Colors.Text);
            for (int dx = 0; dx < layer.Width; dx++)
            {
                for (int dy = 0; dy < layer.Height; dy++)
                {
                    int newX = Camera.X + dx;
                    int newY = Camera.Y + dy;

                    if (newX >= Width || newY >= Height)
                        continue;

                    Tile tile = Field[newX, newY];
                    if (!tile.IsExplored)
                        continue;

                    if (tile.IsVisible)
                    {
                        tile.Draw(layer);
                    }
                    else if (tile.IsWall)
                    {
                        Terminal.Color(Colors.WallBackground);
                        layer.Put(dx, dy, '#');
                    }
                    else
                    {
                        Terminal.Color(Colors.FloorBackground);
                        layer.Put(dx, dy, '.');
                    }
                }
            }

            Exit.MatchSome(exit =>
            {
                if (Camera.OnScreen(exit) && Field[exit].IsVisible)
                {
                    Terminal.Color(Colors.Exit);
                    layer.Put(exit.X - Camera.X, exit.Y - Camera.Y, '>');
                }
            });

            //foreach (Item item in Items.Values)
            //{
            //    if (Camera.OnScreen(item.Pos) && Field[item.Pos].IsVisible)
            //        item.Draw(layer);
            //}

            foreach (BaseActor unit in Units.Values)
            {
                if (Camera.OnScreen(unit.Pos) && Field[unit.Pos].IsVisible)
                    unit.Draw(layer);
            }
        }
        #endregion

        internal void UpdatePlayerFov()
        {
            // Clear vision from last turn
            // TODO: if we know the last move, we might be able to do an incremental update
            foreach (Tile tile in Field)
            {
                tile.Light = 0;
                tile.LosExists = false;
            }

            Tile origin = Field[BaseGame.Player.Pos];
            origin.Light = 1;
            origin.LosExists = true;

            if (!origin.IsExplored)
            {
                origin.IsExplored = true;
            }

            ComputeFov(BaseGame.Player.Pos, EngineConsts.LIGHT_DECAY, true);
        }

        private void UpdatePlayerMaps()
        {
            _goals.Clear();
            _goals.Enqueue(new LocCost(BaseGame.Player.Pos, 0));

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    PlayerMap[x, y] = -1;
                }
            }

            PlayerMap[BaseGame.Player.Pos.X, BaseGame.Player.Pos.Y] = 0;

            ProcessDijkstraMaps(_goals, PlayerMap);
        }

        private void ProcessDijkstraMaps(Queue<LocCost> goals, int[,] mapWeights)
        {
            while (goals.Count > 0)
            {
                LocCost p = goals.Dequeue();

                foreach (Loc next in GetPointsInRadius(p.Loc, 1, _measure))
                {
                    int newCost = p.Cost + 1;
                    Tile tile = Field[next];

                    if (!tile.IsWall && tile.IsExplored
                        && (mapWeights[next.X, next.Y] == -1 || newCost < mapWeights[next.X, next.Y]))
                    {
                        mapWeights[next.X, next.Y] = newCost;
                        goals.Enqueue(new LocCost(next, newCost));
                    }
                }
            }
        }

        internal int ToIndex(in Loc pos) => pos.X + Width * pos.Y;
    }
}
