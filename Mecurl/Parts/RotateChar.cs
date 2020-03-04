namespace Mecurl.Parts
{
    public class RotateChar
    {
        public char Char { get; set; }
        public RotateChar Left { get; set; }
        public RotateChar Right { get; set; }

        public RotateChar(char c)
        {
            Char = c;
            Left = this;
            Right = this;
        }

        public RotateChar(char c, char d)
        {
            Char = c;

            var next = new RotateChar(d);
            Left = next;
            Right = next;
            next.Left = this;
            next.Right = this;
        }

        public RotateChar(char c, char d, char e, char f)
        {
            Char = c;

            var next1 = new RotateChar(d);
            var next2 = new RotateChar(e);
            var next3 = new RotateChar(f);

            Left = next3;
            Right = next1;
            next1.Left = this;
            next1.Right = next2;
            next2.Left = next1;
            next2.Right = next3;
            next3.Left = next2;
            next3.Right = this;
        }
    }

    public static class RotateCharLiterals
    {
        public static RotateChar b1 = new RotateChar('░');
        public static RotateChar b2 = new RotateChar('▒');
        public static RotateChar b3 = new RotateChar('▓');
        public static RotateChar b4 = new RotateChar('█');
        public static RotateChar em = new RotateChar(' ');
        public static RotateChar at = new RotateChar('@');
        public static RotateChar hz = new RotateChar('-', '|');
        public static RotateChar vt = hz.Right;
        public static RotateChar sl = new RotateChar('\\', '/');
        public static RotateChar sr = sl.Right;
        public static RotateChar arn = new RotateChar((char)0xE000, (char)0xE001, (char)0xE002, (char)0xE003);
        public static RotateChar are = arn.Right;
        public static RotateChar ars = are.Right;
        public static RotateChar arw = ars.Right;
    }
}
