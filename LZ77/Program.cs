using System;

namespace LZ77
{
    class Program
    {

        static void Main(string[] args)
        {
            string InputName = string.Empty;
            string OutputName = "lz77default";
            if(args.Length == 1)
            {
                InputName = args[0];
                LZ77Manager lz77 = new LZ77Manager();
                lz77.CompressStream()
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
