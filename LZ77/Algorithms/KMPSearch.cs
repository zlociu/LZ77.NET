using System;
using System.Collections.Generic;
using System.Text;
using LZ77.Models;

namespace LZ77.Algorithms
{
    static internal class KMPSearch
    {
        /// <summary>
        /// Bulds a table that allows the search algorithm to work.
        /// Use before using the search function each time the <paramref name="buffer"/> changes.
        /// </summary>
        /// <param name="buffer"></param>
        private static int[] BuildSearchTable(ReadOnlySpan<char> buffer)
        {
            int[] tab = new int[buffer.Length];

            int i = 2, j = 0;
            tab[0] = -1; tab[1] = 0;

            while(i < buffer.Length)
            {
                if(buffer[i - 1] == buffer[j])
                {
                    tab[i] = j + 1;
                    ++i;
                    ++j;
                }
                else
                {
                    if(j > 0)
                    {
                        j = tab[j];
                    }
                    else
                    {
                        tab[i] = 0;
                        ++i;
                    }
                }
            }
            return tab;
        }

        /// <summary>
        /// Finds the longest matching pattern from <paramref name="buffer"/> inside <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="buffer"></param>
        /// <returns>Lz77CoderOutputModel</returns>
        public static Lz77CoderOutputModel KMPGetLongestMatch(ReadOnlySpan<char> dictionary, ReadOnlySpan<char> buffer)
        {
            
            if (buffer.Length == 0) throw new IndexOutOfRangeException();

            var tab = BuildSearchTable(buffer);

            int m = 0;  // Beginning of the first fit in dictionary
            int i = 0;  // Position of the current char in buffer

            int bestPos = 0;
            int bestLength = 0;

            while(m + i < dictionary.Length)
            {
                if(buffer[i] == dictionary[m + i])
                {
                    ++i;

                    if(i == buffer.Length - 1)
                    {
                        return new Lz77CoderOutputModel{ Position = (ushort)m, Length = (byte)i, Character = buffer[i] };
                    }

                    if (i > bestLength)
                    {
                        bestLength = i;
                        bestPos = m;
                    }
                }
                else
                {
                    m = m + i - tab[i];
                    if(i > 0)
                    {
                        i = tab[i];
                    }
                }
            }

           return new Lz77CoderOutputModel{ Position = (ushort)bestPos, Length = (byte)bestLength, Character = buffer[bestLength] };
        }
    }
}
