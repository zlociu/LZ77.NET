using System;
using System.Collections.Generic;
using System.Text;

namespace LZ77.Algorithms
{
    public enum ShiftDirection
    {
        Left = 0,
        Right = 1
    };

    public static class ArrayExtension
    {
        /// <summary>
        /// Shift elements in array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">array</param>
        /// <param name="offset">how many position should shift</param>
        /// <param name="direction">direction: left | right</param>
        /// <returns>returns new array</returns>
        public static T[] ShiftElements<T>(T[] array, int offset, ushort elementsCount, ShiftDirection direction = ShiftDirection.Right)
        {
            T[] arr = new T[array.Length];
            if (direction == ShiftDirection.Left)
            {
                ArraySegment<T> segment = new ArraySegment<T>(array, offset, Math.Min(elementsCount, array.Length-offset));
                Array.Copy(segment.Array, 0, arr, 0, elementsCount);
            }
            else // ShiftDirection.Right
            {
                ArraySegment<T> segment = new ArraySegment<T>(array, 0, Math.Min(elementsCount, array.Length - offset));
                Array.Copy(segment.Array, 0, arr, offset, elementsCount);
            }
            return arr;
        }
    }
}
