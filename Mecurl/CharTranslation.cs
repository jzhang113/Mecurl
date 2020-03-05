using System;
using System.Collections.Generic;
using System.Text;

namespace Mecurl
{
    static class CharTranslation
    {
        // cp437 and unicode codepoints don't line up outside of the range between 32 and 126
        // (aka your standard letters and characters). Since we care about some of these, we
        // need a translation mapping
        // to make things worse, our ttf doesn't necessarily support all unicode codepoints
        // so some of these mappings are to custom font tiles, with codepoints assigned in
        // Game.cs
        public static int ToUnicode(int cp437)
        {
            if (cp437 >= 32 && cp437 < 127)
                return cp437;

            return cp437 switch
            {
                // arrows
                16 => 0xE011,
                17 => 0xE013,
                30 => 0xE010,
                31 => 0xE012,
                24 => 0xE000,
                25 => 0xE002,
                26 => 0xE001,
                27 => 0xE003,
                // blocks
                176 => '░',
                177 => '▒',
                178 => '▓',
                219 => '█',
                220 => '▄',
                221 => '▌',
                222 => '▐',
                223 => '▀',
                // box-drawing
                179 => '│',
                180 => '┤',
                181 => '╡',
                182 => '╢',
                183 => '╖',
                184 => '╕',
                185 => '╣',
                186 => '║',
                187 => '╗',
                188 => '╝',
                189 => '╜',
                190 => '╛',
                191 => '┐',
                192 => '└',
                193 => '┴',
                194 => '┬',
                195 => '├',
                196 => '─',
                197 => '┼',
                198 => '╞',
                199 => '╟',
                200 => '╚',
                201 => '╔',
                202 => '╩',
                203 => '╦',
                204 => '╠',
                205 => '═',
                206 => '╬',
                207 => '╧',
                208 => '╨',
                209 => '╤',
                210 => '╥',
                211 => '╙',
                212 => '╘',
                213 => '╒',
                214 => '╓',
                215 => '╫',
                216 => '╪',
                217 => '┘',
                218 => '┌',
                _ => 0,
            };
        }
    }
}
