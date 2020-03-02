using Optional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public static class LinqExtensions
    {
        public static Option<T> Random<T>(this IEnumerable<T> enumerable, Random rand)
        {
            var list = enumerable.ToList();
            if (list.Count == 0)
                return Option.None<T>();
            else
                return Option.Some(list[rand.Next(list.Count)]);
        }
    }
}
