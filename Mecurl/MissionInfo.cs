using Mecurl.Parts;

namespace Mecurl
{
    public class MissionInfo
    {
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public int Difficulty { get; set; }

        public MissionType MissionType { get; set; }
        public int Enemies { get; set; }
        public Part RewardPart { get; set; }
        public int RewardScrap { get; set; }
    }

    public enum MissionType
    {
        Elim
    }
}
