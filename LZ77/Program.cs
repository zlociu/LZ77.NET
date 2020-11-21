using System;
using System.IO;
using System.Text;
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
                InputName = args[0] + ".txt";
                OutputName = InputName;
                Lz77Compressor lz77 = new Lz77Compressor();
                lz77.CompressStream(new BinaryReader(File.OpenRead(InputName)), OutputName);
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
            

            //var coderOut = KMPSearch.KMPGetLongestMatch(Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes("ABR")), Encoding.ASCII.GetChars(Encoding.ASCII.GetBytes("ABRACA")));
            //Console.WriteLine("{0} {1} {2}",coderOut?.Position, coderOut?.Length, coderOut?.Character);
        }
    }
}
