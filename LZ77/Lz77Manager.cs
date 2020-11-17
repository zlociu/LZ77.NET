using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LZ77
{
    public sealed class LZ77Manager
    {
        public int DictionarySize { get; private set; }
        public int BufferSize { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictionary">Size of dictionary, should be larger than buffer size</param>
        /// <param name="buffer">Size of buffer</param>
        public LZ77Manager(int dictionary = 1024, int buffer = 64)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">binary stream, source of data to compress</param>
        /// <param name="fileName">filename where compressed file will be saved</param>
        public void CompressStream(BinaryReader stream, string fileName)
        {

        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="stream">binary stream, source of data to decompress</param>
        /// <param name="fileName">filename where decompressed file will be saved</param>
        public void DecompressStream(BinaryReader stream, string fileName)
        {

        }

    }
}
