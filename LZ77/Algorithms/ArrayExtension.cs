﻿using System;
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
        public static T[] ShiftElements<T>(T[] array, int offset, ushort elementsCount, ShiftDirection direction)
        {
            T[] arr = new T[array.Length];
            var cnt = Math.Min(elementsCount, array.Length - offset);
            if (direction == ShiftDirection.Left)
            {
                Array.Copy(array, offset, arr, 0, cnt);
            }
            else // ShiftDirection.Right
            {
                Array.Copy(array, 0, arr, offset, cnt);
            }
            return arr;
        }
    }
}
