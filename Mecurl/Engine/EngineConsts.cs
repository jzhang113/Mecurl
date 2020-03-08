namespace Engine
{
    internal static class EngineConsts
    {
        // FOV and lighting stuff
        public const float MIN_VISIBLE_LIGHT_LEVEL = 0.01f;
        public const double LIGHT_DECAY = 0.06;

        // UI constants
        public const int MAP_WIDTH = 100;
        public const int MAP_HEIGHT = 100;
        public const int MAPVIEW_WIDTH = 100;
        public const int MAPVIEW_HEIGHT = 75;
        public const int SIDEBAR_WIDTH = 20;
        public const int SIDEBAR_R_WIDTH = 15;
        public const int MESSAGE_HEIGHT = 5;

        public const int SCREEN_WIDTH = MAPVIEW_WIDTH + SIDEBAR_WIDTH + SIDEBAR_R_WIDTH + 2;
        public const int SCREEN_HEIGHT = MAPVIEW_HEIGHT + MESSAGE_HEIGHT + 1;

        public const char HEADER_LEFT = '─';  // 196
        public const char HEADER_RIGHT = '─';
        public const char HEADER_SEP = '│';   // 179

        // misc constants
        public const int MESSAGE_HISTORY_COUNT = 100;
        public const Measure MEASURE = Measure.Chebyshev;
        public const int TURN_TICKS = 120;

        // game-specific constants
        internal const double HEAT_DAMAGE = 10;
        internal static double COOL_POWER = 1;
        internal static double COOL_USE_AMT = 10;
        internal static int COOL_USE_TICKS = 120;
    }
}
