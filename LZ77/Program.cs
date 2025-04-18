using LZ77.Algorithms;
using LZ77.Interfaces;
using System;
using System.Diagnostics;

namespace LZ77;

internal class ArgOptions
{
    public bool Compress { get; set; }
    public bool Decompress { get; set; }
    public bool MeasureTime { get; set; }
    public int BufferSize { get; set; }
    public string? OutputFile { get; set; }
    public string InputFile { get; set; }

    public ArgOptions()
    {
        Compress = false;
        Decompress = false;
        MeasureTime = false;
        BufferSize = 64;
        OutputFile = null;
        InputFile = string.Empty;
    }
}

class Program
{
    private static void PrintErrorCommand()
    {
        Console.WriteLine("[-d | -c] <filename> [-b <value>] [-o <filename>]");
        Console.WriteLine("-h \t\t Show this help menu");
        Console.WriteLine("-d \t\t Decompress specified file");
        Console.WriteLine("-c \t\t Compress specified file");
        Console.WriteLine("-b <int value> \t Buffer size (32, 64, 128, 256), default: 64");
        Console.WriteLine("-o <filename> \t Output filename");
        Console.WriteLine("-t \t\t Measure time");
    }

    static void Main(string[] args)
    {
        string[] parameters = args;
        ArgOptions options = new();
        for (int i = 0; i < parameters.Length;)
        {
            if (parameters[i].StartsWith('-'))
            {
                switch (parameters[i])
                {
                    case "-d":
                        {
                            if (options.Compress == false) options.Decompress = true;
                            i++;
                        }
                        break;
                    case "-c":
                        {
                            if (options.Decompress == false) options.Compress = true;
                            i++;
                        }
                        break;
                    case "-b":
                        {
                            try
                            {
                                options.BufferSize = int.Parse(parameters[i + 1]);
                                i += 2;
                            }
                            catch (Exception)
                            {
                                PrintErrorCommand();
                                return;
                            }
                        }
                        break;
                    case "-o":
                        {
                            try
                            {
                                if (parameters[i + 1] == "--")
                                {
                                    options.OutputFile = parameters[i + 2];
                                    i += 3;
                                    break;
                                }
                                if (parameters[i + 1].StartsWith('-'))
                                    throw new ArgumentException(nameof(ArgOptions.OutputFile));
                                options.OutputFile = parameters[i + 1];
                                i += 2;
                            }
                            catch (Exception)
                            {
                                PrintErrorCommand();
                                return;
                            }
                        }
                        break;
                    case "-t":
                        {
                            options.MeasureTime = true;
                            i++;
                        }
                        break;
                    case "-h":
                        {
                            PrintErrorCommand();
                            return;
                        }
                    case "--":
                        {
                            options.InputFile = parameters[i + 1];
                            i += 2;
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
                if (string.IsNullOrEmpty(options.InputFile))
                {
                    options.InputFile = parameters[i];
                    i++;
                    continue;
                }
                PrintErrorCommand();
                return;
            }
        }

        ICompressor lz77;

        if (options.BufferSize == 32 ||
            options.BufferSize == 64 ||
            options.BufferSize == 128 ||
            options.BufferSize == 256)
        {
            lz77 = new Lz77Compressor((Lz77BufferSize)(options.BufferSize - 1));
        }
        else
        {
            Console.WriteLine("Warning: Buffer size is not legal value. Used default (64) buffer size value!");
            lz77 = new Lz77Compressor(Lz77BufferSize.B64);
        }

        if (string.IsNullOrEmpty(options.InputFile))
        {
            Console.WriteLine("Error: No input filename!");
            return;
        }

        try
        {
            Stopwatch s1 = new();
            s1.Start();
            if (options.Compress) lz77.CompressFile(options.InputFile, options.OutputFile);
            if (options.Decompress) lz77.DecompressFile(options.InputFile, options.OutputFile);
            s1.Stop();
            if (options.MeasureTime) Console.WriteLine("Time: {0}ms", s1.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}

