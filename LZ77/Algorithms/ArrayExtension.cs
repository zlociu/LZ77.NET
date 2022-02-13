using System;
using System.Collections.Generic;
using System.Text;

namespace LZ77.Algorithms
{
    public static class ArrayExtension
    {
        /// <summary>
        /// Shift elements in array, delete <paramref name="offset"/> elements from left/right side
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">array</param>
        /// <param name="offset">how many position should shift</param>
        /// <param name="elementsCount">how many elements are in array (to improve efficiency)</param>
        /// <returns>returns new array</returns>
        public static T[] LeftShiftElements<T>(T[] array, int offset, int elementsCount)
        {
            var arr = new T[array.Length];
            var cnt = Math.Min(elementsCount, array.Length - offset);

            Array.Copy(array, offset, arr, 0, cnt); 
            return arr;
        }
    }
}
