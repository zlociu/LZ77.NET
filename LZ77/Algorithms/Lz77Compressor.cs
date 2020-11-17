using LZ77.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LZ77.Models;
using LZ77.Algorithms;
using static LZ77.Algorithms.ArrayExtension;

namespace LZ77.Algorithms
{
    

    public sealed class Lz77Compressor: ICompressor
    {
        public ushort DictionarySize { get; private set; }
        public ushort BufferSize { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="substringBinaryLenght">lenght in bits of substring lenght value</param>
        /// <returns></returns>
        private Lz77CoderOutputModel ConvertNumberToCoderOutput(ushort number, ushort substringBinaryLenght)
        {
            Lz77CoderOutputModel model = new Lz77CoderOutputModel();
            model.Lenght = (ushort)(number % (short) Math.Pow(2, substringBinaryLenght));
            model.Position = (ushort)((number >> (16 - substringBinaryLenght)) % (ushort)Math.Pow(2, 16-substringBinaryLenght));
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="substringBinaryLenght">lenght in bits of substring lenght value</param>
        /// <returns></returns>
        private ushort ConvertCoderOutputToNumber(Lz77CoderOutputModel model, ushort substringBinaryLenght)
        {
            ushort number;
            number = model.Lenght;
            number += (ushort)(model.Position << substringBinaryLenght);
            return number;
        }

        private Lz77CoderOutputModel GetLongestSubstring(ref char[] dictionary, ref char[] buffer)
        {
            var output = new Lz77CoderOutputModel();
            return output;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dictionary">Size of dictionary, should be larger than buffer size</param>
        /// <param name="buffer">Size of buffer</param>
        public Lz77Compressor(int dictionary = 1024, int buffer = 64)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to compress</param>
        /// <param name="fileName">filename where compressed file will be saved</param>
        public void CompressStream(BinaryReader stream, string fileName)
        {
            var dictionary = new char[DictionarySize];
            var buffer = new char[BufferSize];

            var outputFile = File.Create(fileName + ".lz77");
            var outputStream = new BinaryWriter(outputFile);

            int n;

            buffer = stream.ReadChars(BufferSize);
            do
            {
                //1. znajdź najdłuższy ciąg w 'buffer' ktory istnieje w 'dictionary'
                //2. wyznacz (P,C,'a') 
                //      P - index gdzie się zaczyna ciąg w 'dictionary'
                //      C - długość ciągu
                //      'a' - następny znak w 'buffer' po tym ciągu
                //3. przesuń 'dictionary' o C + 1 pozycji w prawo
                //4. skopiuj C + 1 elementów z buffer do 'dictionary'
                //5. przesuń C + 1 elementów w lewo w 'buffer'
                //6. dodaj C + 1 nowych elementów ze 'stream' do 'buffer'
                //7. dodaj do pliku wyjsciowego C + (P << bitLen(C)) oraz 'a' jako liczba i znak
                var coderOut = GetLongestSubstring(ref dictionary, ref buffer);

                dictionary = ArrayExtension.ShiftElements(dictionary, (coderOut.Lenght + 1));
                Array.Copy(buffer, 0, dictionary, 0, coderOut.Lenght + 1);
                buffer = ArrayExtension.ShiftElements(buffer, (coderOut.Lenght + 1), ShiftDirection.Left);
                var tmp = stream.ReadChars(coderOut.Lenght + 1);
                Array.Copy(tmp, 0, buffer, (BufferSize - coderOut.Lenght - 1), (coderOut.Lenght + 1));
                n = tmp.Length;

                var number = ConvertCoderOutputToNumber(coderOut, (ushort)((ushort)(Math.Log2(BufferSize)) + 1));
                outputStream.Write(number);
                outputStream.Write(coderOut.Character);

            } while (n != 0);
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to decompress</param>
        /// <param name="fileName">filename where decompressed file will be saved</param>
        public void DecompressStream(BinaryReader stream, string fileName)
        {

        }

    }
}
