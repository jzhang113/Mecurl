using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Commands;
using Mecurl.Parts;
using Optional;
using System.Collections.Generic;
using System.Drawing;

namespace Mecurl.Actors
{
    public class Actor : BaseActor
    {
        public Loc Facing { get; set; }
        public ICollection<Part> Parts { get; }

        public Actor(in Loc pos, int hp, char symbol, Color color) : base(pos, hp, symbol, color)
        {
            Facing = Direction.N;
            Parts = new List<Part>()
            {
                new Part() { Name = "Core", Structure = new char[3, 3] { { '/', '█', '\\' }, { '█', '@', '█' }, { '\\', '█', '/' } } },
                new Part() { Name = "Shield", Structure = new char[5, 1] { { '*' }, { '*' }, { '*' }, { '*' }, { '*' } }, AttachPos = new Loc(0, -2) },
            };
        }

        public override Option<ICommand> TriggerDeath()
        {
            Game.MapHandler.RemoveActor(this);

            if (Game.MapHandler.Field[Pos].IsVisible)
            {
                Game.MessagePanel.AddMessage($"{Name} dies");
                Game.MapHandler.Refresh();
            }

            return Option.None<ICommand>();
        }

        public override Option<ICommand> GetAction()
        {
            return Option.Some<ICommand>(new WaitCommand(this));
        }

        public override void Draw(LayerInfo layer)
        {

            if (!ShouldDraw)
                return;

            if (IsDead)
            {
                Terminal.Color(Swatch.DbOldBlood);
                layer.Put(Pos.X - Camera.X, Pos.Y - Camera.Y, '%');
                return;
            }


            Terminal.Color(Color);

            foreach (Part p in Parts)
            {
                if (Facing == Direction.N)
                {
                    int topleftX = p.AttachPos.X - p.Structure.GetLength(0) / 2;
                    int topleftY = p.AttachPos.Y - p.Structure.GetLength(1) / 2;

                    for (int x = 0; x < p.Structure.GetLength(0); x++)
                    {
                        for (int y = 0; y < p.Structure.GetLength(1); y++)
                        {
                            layer.Put(
                                topleftX + x + Pos.X - Camera.X,
                                topleftY + y + Pos.Y - Camera.Y,
                                p.Structure[x, y]);
                        }
                    }
                }
                else if (Facing == Direction.S)
                {
                    int topleftX = -p.AttachPos.X - p.Structure.GetLength(0) / 2;
                    int topleftY = -p.AttachPos.Y - p.Structure.GetLength(1) / 2;

                    for (int x = 0; x < p.Structure.GetLength(0); x++)
                    {
                        for (int y = 0; y < p.Structure.GetLength(1); y++)
                        {
                            layer.Put(
                                topleftX + x + Pos.X - Camera.X,
                                topleftY + y + Pos.Y - Camera.Y,
                                p.Structure[x, y]);
                        }
                    }
                }
                else if (Facing == Direction.E)
                {
                    int topleftX = -p.AttachPos.Y - p.Structure.GetLength(1) / 2;
                    int topleftY = p.AttachPos.X - p.Structure.GetLength(0) / 2;

                    for (int x = 0; x < p.Structure.GetLength(0); x++)
                    {
                        for (int y = 0; y < p.Structure.GetLength(1); y++)
                        {
                            layer.Put(
                                topleftX + y + Pos.X - Camera.X,
                                topleftY + x + Pos.Y - Camera.Y,
                                p.Structure[x, y]);
                        }
                    }
                }
                else if (Facing == Direction.W)
                {
                    int topleftX = p.AttachPos.Y - p.Structure.GetLength(1) / 2;
                    int topleftY = -p.AttachPos.X - p.Structure.GetLength(0) / 2;

                    for (int x = 0; x < p.Structure.GetLength(0); x++)
                    {
                        for (int y = 0; y < p.Structure.GetLength(1); y++)
                        {
                            layer.Put(
                                topleftX + y + Pos.X - Camera.X,
                                topleftY + x + Pos.Y - Camera.Y,
                                p.Structure[x, y]);
                        }
                    }
                }
                else
                {
                    System.Console.WriteLine($"actor {Id} has invalid facing");
                }
            }
        }
    }
}
