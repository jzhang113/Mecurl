using BearLib;
using Engine;
using Engine.Drawing;
namespace Mecurl.UI
{
    internal static class RadarPanel
    {
        public static void Draw(LayerInfo layer)
        {
            // draw borders
            Terminal.Color(Colors.BorderColor);
            layer.DrawBorders(new BorderInfo
            {
                TopLeftChar = '┬',
                TopRightChar = '┐',
                BottomLeftChar = '├',
                BottomRightChar = '┘',
                TopChar = '─', // 196
                BottomChar = '─',
                RightChar = '│' // 179
            });

            var map = Game.MapHandler;

            Loc midPos = Game.Player.Pos;
            const int scale = 5;
            int radius = layer.Width / 2 + 1;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y >= radius * radius) continue;

                    int topLeftX = midPos.X + x * scale;
                    int topLeftY = midPos.Y + y * scale;
                    char symbol = '#';
                    var color = Colors.Text;

                    for (int i = 0; i < scale * scale; i++)
                    {
                        int subX = i % scale;
                        int subY = i / scale;
                        var pos = new Loc(topLeftX + subX, topLeftY + subY);
                        if (!map.Field.IsValid(pos)) continue;

                        if (map.Units.TryGetValue(map.ToIndex(pos), out BaseActor actor))
                        {
                            if (actor == Game.Player)
                            {
                                color = Colors.Player;
                                symbol = '@';
                                break;
                            }
                            else
                            {
                                color = System.Drawing.Color.Red;
                                symbol = actor.Symbol;
                                break;
                            }
                        }

                        // if there is at least one valid position, the symbol will be . instead of #
                        symbol = '.';
                    }

                    Terminal.Color(color);
                    layer.Put(x + radius - 1, y + radius - 1, symbol);
                }
            }
        }
    }
}
