using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LZ77.Interfaces
{
    public interface ICompressor
    {

        void CompressStream(BinaryReader stream, string outputFileName);
        void DecompressStream(BinaryReader stream, string outputFileName);
    }
}
