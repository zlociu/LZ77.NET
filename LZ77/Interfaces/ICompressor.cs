using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LZ77.Interfaces
{
    public interface ICompressor
    {
        void CompressFile(string fileName);
        void DecompressFile(string fileName, string? outputFileName);
    }
}
