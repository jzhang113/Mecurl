using BearLib;
using Engine;
using Engine.Drawing;
using Mecurl.Engine;
using System;
using System.Collections.Generic;

namespace Mecurl.UI
{
    // Accepts messages from various systems and displays it in the message console. Also keeps a 
    // rolling history of messages that can be displayed.
    public class MessagePanel : IMessageHandler
    {
        private readonly int _maxSize;
        private readonly IList<string> _messages;

        public MessagePanel(int maxSize)
        {
            _maxSize = maxSize;
            _messages = new List<string>();
        }

        public void Add(string text)
        {
            _messages.Add(text);

            if (_messages.Count > _maxSize)
                _messages.RemoveAt(0);
        }

        public void Append(string text)
        {
            int prev = _messages.Count - 1;
            _messages[prev] += " " + text;
        }

        public void Clear()
        {
            _messages.Clear();
        }

        public void Draw(LayerInfo layer)
        {
            // draw borders
            Terminal.Color(Colors.BorderColor);
            layer.DrawBorders(new BorderInfo
            {
                TopLeftChar = '╞',
                TopRightChar = '│',
                LeftChar = '│',
                RightChar = '│',
                BottomChar = '─',
            });

            Terminal.Color(Colors.Text);
            Terminal.Font("message");

            // draw messages
            int maxCount = Math.Min(_messages.Count, layer.Height);
            int yPos = layer.Height - 1;

            for (int i = 0; i < maxCount; i++)
            {
                layer.Print(yPos, _messages[_messages.Count - i - 1]);
                yPos--;
            }

            Terminal.Font("");
        }
    }
}
