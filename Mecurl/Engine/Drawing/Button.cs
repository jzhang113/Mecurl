using BearLib;
using System;
using System.Drawing;

namespace Engine.Drawing
{
    public class Button
    {
        public Rectangle Position { get; }
        public string Text { get; }

        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }
        public Color HoverColor { get; set; }
        public Color TextColor { get; set; }
        public Color DisabledColor { get; set; }

        internal bool Hover { get; set; }
        internal bool Disabled { get; set; }

        private readonly Action _callback;
        private readonly bool _border;
        private readonly ContentAlignment _align;

        public Button(Rectangle position, string text, Action callback,
            bool disabled = false, bool border = false, ContentAlignment alignment = ContentAlignment.MiddleCenter)
        {
            Position = position;
            Text = text;
            _callback = callback;
            Disabled = disabled;

            BackgroundColor = Colors.ButtonBackground;
            BorderColor = Colors.ButtonBorder;
            HoverColor = Colors.ButtonHover;
            TextColor = Colors.Text;
            DisabledColor = Color.Gray;

            _border = border;
            _align = alignment;
        }

        public void Press()
        {
            if (!Disabled)
            {
                _callback();
            }
        }

        public void Draw(LayerInfo layer)
        {
            Terminal.Composition(true);

            Color color;
            if (Disabled) color = DisabledColor;
            else if (Hover) color = HoverColor;
            else color = BackgroundColor;
            Terminal.Color(color);

            if (_border)
            {
                for (int x = Position.Left + 1; x < Position.Right - 1; x++)
                {
                    for (int y = Position.Top + 1; y < Position.Bottom - 1; y++)
                    {
                        layer.Put(x, y, '█'); // 219
                    }
                }

                Terminal.Color(BorderColor);
                // top and bottom border
                for (int x = Position.Left + 1; x < Position.Right - 1; x++)
                {
                    layer.Put(x, Position.Top, '─'); // 196
                    layer.Put(x, Position.Bottom - 1, '─');
                }

                // left and right border
                for (int y = Position.Top + 1; y < Position.Bottom - 1; y++)
                {
                    layer.Put(Position.Left, y, '│'); // 179
                    layer.Put(Position.Right - 1, y, '│');
                }

                // corners
                layer.Put(Position.Left, Position.Top, '┌'); // 218
                layer.Put(Position.Right - 1, Position.Top, '┐'); // 191
                layer.Put(Position.Left, Position.Bottom - 1, '└'); // 192
                layer.Put(Position.Right - 1, Position.Bottom - 1, '┘'); // 217
            }
            else
            {
                for (int x = Position.Left; x < Position.Right; x++)
                {
                    for (int y = Position.Top; y < Position.Bottom; y++)
                    {
                        layer.Put(x, y, '█');
                    }
                }
            }

            Terminal.Color(TextColor);
            layer.Print(Position, Text, _align);
            Terminal.Composition(false);
        }
    }
}
