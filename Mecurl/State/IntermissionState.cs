using BearLib;
using Engine;
using Engine.Drawing;
using Optional;
using System.Drawing;

namespace Mecurl.State
{
    public class IntermissionState : IState
    {
        private const int _bHeight = 7;

        private static double[,] _perlinMap;
        private static double[,] _perlinMap2;

        public IntermissionState()
        {
            var _gen = new Perlin();
            _perlinMap = new double[95, 67];
            _perlinMap2 = new double[95, 67];
            for (int x = 0; x < 95; x++)
            {
                for (int y = 0; y < 67; y++)
                {
                    _perlinMap[x, y] = _gen.OctavePerlin(x * 0.2 + 5.3, y * 0.15 + 5.9, 14.44, 10, 0.5);
                    _perlinMap2[x, y] = _gen.OctavePerlin(x * 0.4 + 45.3, y * 0.6 + 11.2, 18.7, 10, 0.5);
                }
            }
        }

        public Option<ICommand> HandleKeyInput(int key)
        {
            return Option.None<ICommand>();
        }

        public Option<ICommand> HandleMouseInput(double x, double y, ClickState click)
        {
            return Option.None<ICommand>();
        }

        public void Draw(LayerInfo layer)
        {
            // overworld map
            Terminal.Layer(2);
            layer.Print(1, 1, $"Overworld map");
            Terminal.Layer(1);
            for (int i = 0; i < 95; i++)
            {
                for (int j = 0; j < 67; j++)
                {
                    if (_perlinMap[i, j] < 0.37)
                    {
                        Terminal.Color(Color.Blue);
                    }
                    else
                    {
                        Terminal.Color(Color.Green);
                    }

                    char c;
                    if (_perlinMap2[i, j] < 0.35)
                    {
                        c = ' ';
                    }
                    else if (_perlinMap2[i, j] < 0.4)
                    {
                        c = '░';
                    }
                    else if (_perlinMap2[i, j] < 0.47)
                    {
                        c = '▒';
                    }
                    else if (_perlinMap2[i, j] < 0.55)
                    {
                        c = '▓';
                    }
                    else
                    {
                        c = '█';
                    }

                    Terminal.Put(2 + i, 2 + j, c);
                }
            }

            // mission briefing
            int buttonBorderY = layer.Height - _bHeight - 5;
            int briefingBorderX = layer.Width - 40;

            Terminal.Color(Colors.Text);
            layer.Print(briefingBorderX + 1, 1, "Mission summary");
            layer.Print(briefingBorderX + 1, 2, "───────────────");

            string briefingString;
            if (Game.Difficulty >= 5)
                briefingString = "The region is peaceful. Congratulations, you have won.";
            else
                briefingString = "Hostile forces have been detected in the region";

            layer.Print(new Rectangle(briefingBorderX + 2, 3, 39, 28), briefingString, ContentAlignment.TopLeft);


            layer.Print(briefingBorderX + 1, 30, "Objectives");
            layer.Print(briefingBorderX + 1, 31, "──────────");

            string objectiveString = "None";
            switch (Game.NextMission.MissionType)
            {
                case MissionType.Elim: objectiveString = "Eliminate all enemies"; break;
            };
            if (Game.Difficulty >= 5)
                objectiveString = "None";
            layer.Print(briefingBorderX + 2, 32, objectiveString);

            layer.Print(briefingBorderX + 1, 60, "Reward");
            layer.Print(briefingBorderX + 1, 61, "──────");

            if (Game.Difficulty >= 5)
            {
                layer.Print(briefingBorderX + 2, 62, $"None");
            }
            else
            {
                int y = 62;
                if (Game.NextMission.RewardPart != null)
                    layer.Print(briefingBorderX + 2, y++, $"{Game.NextMission.RewardPart.Name}");

                if (Game.NextMission.RewardScrap > 0)
                    layer.Print(briefingBorderX + 2, y++, $"{Game.NextMission.RewardScrap} scrap");
            }

            // borders and buttons
            Terminal.Color(Colors.BorderColor);
            for (int dx = 1; dx < layer.Width - 1; dx++)
            {
                layer.Put(dx, buttonBorderY, '═');
            }

            for (int dy = 1; dy < buttonBorderY; dy++)
            {
                layer.Put(briefingBorderX, dy, '║');
            }

            layer.Put(briefingBorderX, buttonBorderY, '╩');
        }
    }
}
