using Mecurl;
using System;

namespace Engine.Drawing
{
    internal static class Camera
    {
        public static int X { get; private set; }
        public static int Y { get; private set; }

        internal static void UpdateCamera(in Loc center)
        {
            const int screenWidth = EngineConsts.MAPVIEW_WIDTH;
            const int screenHeight = EngineConsts.MAPVIEW_HEIGHT;

            // set left and top limits for the camera
            int startX = Math.Max(center.X - (screenWidth / 2), 0);
            int startY = Math.Max(center.Y - (screenHeight / 2), 0);

            // set right and bottom limits for the camera
            int xDiff = Game.MapHandler.Width - screenWidth;
            int yDiff = Game.MapHandler.Height - screenHeight;
            X = xDiff < 0 ? 0 : Math.Min(xDiff, startX);
            Y = yDiff < 0 ? 0 : Math.Min(yDiff, startY);
        }

        internal static bool OnScreen(in Loc pos)
        {
            (int x, int y) = pos;
            return x >= X && x < X + EngineConsts.MAPVIEW_WIDTH && y >= Y && y < Y + EngineConsts.MAPVIEW_HEIGHT;
        }
    }
}