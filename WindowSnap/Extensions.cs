using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowSnap
{
    public static class Extensions
    {
        public static int Set<T>(this T[] array, T element)
        {
            if (array.Contains(element))
            {
                return Array.IndexOf(array, element);
            }
            else
            {
                for (var i = 0; i < array.Length; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(array[i], default(T)))
                    {
                        array[i] = element;
                        return i;
                    }
                }
            }
            return -1;
        }
        public static bool Unset<T>(this T[] array, T element)
        {
            var index = Array.IndexOf(array, element);
            if (index >= 0)
            {
                array[index] = default(T);
                return true;
            }
            return false;
        }
    }
}
