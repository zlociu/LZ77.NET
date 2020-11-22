using System;
using System.IO;
using System.Text;
using LZ77.Algorithms;
using LZ77.Interfaces;

namespace LZ77
{
    class Program
    {

        static void Main(string[] args)
        {
            
            string InputName;
            string OutputName;

            if(args.Length == 1)
            {
                InputName = args[0] + ".txt";
                OutputName = InputName;
                ICompressor lz77 = new Lz77Compressor();

                try
                {
                    //lz77.CompressStream(new BinaryReader(File.OpenRead(InputName)), OutputName);
                    //lz77.DecompressStream(new BinaryReader(File.OpenRead(OutputName + ".lz77")), "wynik");
                    lz77.CompressFile(InputName);
                    lz77.DecompressFile(InputName + ".lz77");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
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
