using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using LZ77.Algorithms;
using LZ77.Interfaces;

namespace LZ77
{
    public class ArgOptions
    {
        public bool Compress { get; set; }
        public bool Decompress { get; set; }
        public bool Time { get; set; }
        public int BufferSize { get; set; }
        public string OutputFile { get; set; }
        public string InputFile { get; set; }

        public ArgOptions()
        {
            Compress = false;
            Decompress = false;
            Time = false;
            BufferSize = 64;
            OutputFile = string.Empty;
            InputFile = string.Empty;
        }
    }

    class Program
    {

        static void PrintErrorCommand()
        {
            Console.WriteLine("[-d | -c] <filename> [-b <value>] [-o <filename>]");
            Console.WriteLine("-d \t\t Decompress specified file");
            Console.WriteLine("-c \t\t Compress specified file");
            Console.WriteLine("-b <int value> \t Buffer size (32, 64, 128, 256)");
            Console.WriteLine("-o <filename> \t Output filename");
            Console.WriteLine("-t \t\t Measure time");
        }

        static void Main(string[] args)
        {
            string[] parameters = args;
            ArgOptions options = new ArgOptions();
            for(int i = 0; i < parameters.Length;)
            {
                if(parameters[i].StartsWith('-'))
                {
                    switch(parameters[i])
                    {
                        case "-d":
                            {
                                if (options.Compress == false) options.Decompress = true;   
                                i++;
                            }break;
                        case "-c":
                            {
                                if (options.Decompress == false) options.Compress = true;
                                i++;
                            }break;
                        case "-b": 
                            {
                                try
                                {
                                    options.BufferSize = int.Parse(parameters[i + 1]);
                                    i += 2;
                                }
                                catch(Exception)
                                {
                                    PrintErrorCommand();
                                    return;
                                }
                            }break;
                        case "-o":
                            {
                                try
                                {
                                    options.OutputFile = parameters[i + 1];
                                    i += 2;
                                }
                                catch(Exception)
                                {
                                    PrintErrorCommand();
                                    return;
                                }
                            }
                            break;
                        case "-t":
                            {
                                options.Time = true;
                                i++;
                            }
                            break;
                        default:
                            {
                                PrintErrorCommand();
                                return;
                            }
                    }
                }
                else
                {
                    if (options.InputFile == string.Empty)
                    {
                        options.InputFile = parameters[i];
                        i++;
                    }
                    else
                    {
                        PrintErrorCommand();
                        return;
                    }
                }
            }

            ICompressor lz77;

            if (options.BufferSize == 32 || options.BufferSize == 64 || options.BufferSize == 128 || options.BufferSize == 256)
            {
                
                lz77 = new Lz77Compressor((Lz77BufferSize)options.BufferSize);
            }
            else
            {
                Console.WriteLine("Warning: Buffer size is not legal value. Used default buffer size value!");
                lz77 = new Lz77Compressor(Lz77BufferSize.B64);
            }
            
            if(options.InputFile == string.Empty)
            {
                Console.WriteLine("Error: No input filename!");
                return;
            }

            Stopwatch s1 = new Stopwatch();
            try
            {
                s1.Start();
                if(options.Compress)    lz77.CompressFile(options.InputFile);
                if(options.Decompress)  lz77.DecompressFile(options.InputFile, options.OutputFile);
                s1.Stop();
                if(options.Time) Console.WriteLine("Time: {0}ms",s1.ElapsedMilliseconds);
                

                //lz77.CompressFile(InputName);
                //lz77.DecompressFile(InputName + ".lz77");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
