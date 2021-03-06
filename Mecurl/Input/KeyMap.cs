﻿using System.Collections.Generic;

namespace Mecurl.Input
{
    internal struct KeyMap
    {
        public StateMap<NormalInput> NormalMap { get; set; }
        public StateMap<TargettingInput> TargettingMap { get; set; }

        internal struct StateMap<T>
        {
            public IDictionary<int, T> Shift { get; set; }
            public IDictionary<int, T> Ctrl { get; set; }
            public IDictionary<int, T> Alt { get; set; }
            public IDictionary<int, T> None { get; set; }
        }
    }
}
