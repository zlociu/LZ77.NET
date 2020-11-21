using System;
using System.Collections.Generic;
using System.Text;
using LZ77.Models;

namespace LZ77.Algorithms
{
    static class KMPSearch
    {
        /// <summary>
        /// Bulds a table that allows the search algorithm to work.
        /// Use before using the search function each time the <paramref name="buffer"/> changes.
        /// </summary>
        /// <param name="buffer"></param>
        private static int[] BuildSearchTable(char[] buffer)
        {
            int[] Tab = new int[buffer.Length];

            int i = 2, j = 0;
            Tab[0] = -1; Tab[1] = 0;

            while(i < buffer.Length)
            {
                if(buffer[i - 1] == buffer[j])
                {
                    Tab[i] = j + 1;
                    ++i;
                    ++j;
                }
                else
                {
                    if(j > 0)
                    {
                        j = Tab[j];
                    }
                    else
                    {
                        Tab[i] = 0;
                        ++i;
                    }
                }
            }
            return Tab;
        }

        /// <summary>
        /// Finds the longest matching pattern from <paramref name="buffer"/> inside <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="buffer"></param>
        /// <returns>Lz77CoderOutputModel</returns>
        public static Lz77CoderOutputModel? KMPGetLongestMatch(char[] dictionary, char[] buffer)
        {
            
            if (buffer.Length == 0) return null;

            var Tab = BuildSearchTable(buffer);

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
                    m = m + i - Tab[i];
                    if(i > 0)
                    {
                        i = Tab[i];
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
