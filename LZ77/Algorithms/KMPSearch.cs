using System;
using System.Collections.Generic;
using System.Text;

namespace LZ77.Algorithms
{
    class KMPSearch
    {
        // Table used to find repeating symbols in the pattern
        private int[] T;

        // Initialize the table size
        KMPSearch(int bufferSize)
        {
            T = new int[bufferSize];
        }

        /// <summary>
        /// Bulds a table that allows the search algorithm to work.
        /// Use before using the search function each time the <paramref name="buffer"/> changes.
        /// </summary>
        /// <param name="buffer"></param>
        public void BuildSearchTable(char[] buffer)
        {
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
    }
}
