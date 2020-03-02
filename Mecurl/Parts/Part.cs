using Engine;

namespace Mecurl.Parts
{
    public class Part
    {
        public string Name { get; set; }
        public char[,] Structure { get; set; }
        public Loc AttachPos { get; set; }
    }
}
