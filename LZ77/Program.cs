using System;
using System.IO;
using LZ77.Algorithms;

namespace LZ77
{
    class Program
    {

        static void Main(string[] args)
        {
            string InputName = string.Empty;
            string OutputName = string.Empty;
            if(args.Length == 1)
            {
                InputName = args[0];
                OutputName = InputName + "_compress";
                Lz77Compressor lz77 = new Lz77Compressor();
                lz77.CompressStream(new BinaryReader(File.OpenRead(InputName + ".txt")), OutputName);
            }
            else
            {
                if(args.Length == 2)
                {
                    InputName = args[0];
                    OutputName = args[1];
                }
                else
                {
                    return;
                }
            }
        }
    }
}
