using System;
using System.Collections.Generic;
using System.Text;
using LZ77.Models;

namespace LZ77.Algorithms
{
    class KMPSearch
    {
        // Table used to find repeating symbols in the pattern
        private static List<int> T;

        /// <summary>
        /// Bulds a table that allows the search algorithm to work.
        /// Use before using the search function each time the <paramref name="buffer"/> changes.
        /// </summary>
        /// <param name="buffer"></param>
        private static void BuildSearchTable(in char[] buffer)
        {
            if(T.Count != buffer.Length)
                T = new List<int>(buffer.Length);

            int i = 2, j = 0;
            T[0] = -1; T[1] = 0;

            while(i < buffer.Length)
            {
                if(buffer[i - 1] == buffer[j])
                {
                    T[i] = j + 1;
                    ++i;
                    ++j;
                }
                else
                {
                    if(j > 0)
                    {
                        j = T[j];
                    }
                    else
                    {
                        T[i] = 0;
                        ++i;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the longest matching pattern from <paramref name="buffer"/> inside <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="buffer"></param>
        /// <returns>Lz77CoderOutputModel</returns>
        public static Lz77CoderOutputModel KMPGetLongestMatch(in char[] dictionary, in char[] buffer)
        {
            BuildSearchTable(buffer);

            var output = new Lz77CoderOutputModel();

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
                        output.Position = (ushort)m;
                        output.Length = (ushort)i;
                        output.Character = buffer[i];
                        return output;
                    }

                    if (i > bestLength)
                    {
                        bestLength = i;
                        bestPos = m;
                    }
                }
                else
                {
                    m = m + i - T[i];
                    if(i > 0)
                    {
                        i = T[i];
                    }
                }
            }

            output.Position = (ushort)bestPos;
            output.Length = (ushort)bestLength;
            output.Character = buffer[bestLength];

            return output;
        }
    }
}
