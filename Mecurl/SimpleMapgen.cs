using Engine.Map;
using System;

namespace Mecurl
{
    public class SimpleMapgen : MapGenerator
    {
        public SimpleMapgen(int width, int height, int level) : base(width, height, level, Game.Rand)
        {
        }

        protected override void CreateMap()
        {
            CreateRoom(new Room(1, 1, Width - 2, Height - 2));
        }

        protected override void PlaceActors()
        {
            Map.AddActor(Game.Player);
        }

        protected override void PlaceItems()
        {
        }
    }
}
