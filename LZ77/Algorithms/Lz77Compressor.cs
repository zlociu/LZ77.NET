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
    public enum Lz77SizeType
    {
        D1_B64,
        D32_B256
    };

    public sealed class Lz77Compressor: ICompressor
    {
        private readonly Lz77SizeType _type;

        private readonly ushort _dictionarySize;
        private readonly ushort _bufferSize;

        private readonly ushort _dictionaryBitLen;
        private readonly ushort _bufferBitLen;

        private ushort _bufferFillNumber;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private Lz77CoderOutputModel ConvertIntToCoderOutput(int number)
        {
            Lz77CoderOutputModel model = new Lz77CoderOutputModel();
            model.Lenght = (ushort)(number % (int)Math.Pow(2, _bufferBitLen));
            model.Position = (ushort)((number >> (_bufferBitLen)) % (int)Math.Pow(2, _dictionaryBitLen));
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private Lz77CoderOutputModel ConvertShortToCoderOutput(ushort number)
        {
            Lz77CoderOutputModel model = new Lz77CoderOutputModel();
            model.Lenght = (ushort)(number % (short) Math.Pow(2, _bufferBitLen));
            model.Position = (ushort)((number >> (_bufferBitLen)) % (ushort)Math.Pow(2, _dictionaryBitLen));
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private ushort ConvertCoderOutputToShort(Lz77CoderOutputModel model)
        {
            ushort number;
            number = model.Lenght;
            number += (ushort)(model.Position << _bufferBitLen);
            return number;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private int ConvertCoderOutputToInt(Lz77CoderOutputModel model)
        {
            int number;
            number = model.Lenght;
            number += (model.Position << _bufferBitLen);
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
        /// <param name="type">Size of dictionary and buffer</param>
        public Lz77Compressor(Lz77SizeType type = Lz77SizeType.D1_B64)
        {
            _type = type;
            switch(type)
            {
                case Lz77SizeType.D1_B64:
                    {
                        _dictionarySize = 1023;
                        _bufferSize = 63;

                        _dictionaryBitLen = 10;
                        _bufferBitLen = 6;
                    }break;
                case Lz77SizeType.D32_B256:
                    {
                        _dictionarySize = 32767;
                        _bufferSize = 255;

                        _dictionaryBitLen = 16;
                        _bufferBitLen = 8;
                    }
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to compress</param>
        /// <param name="fileName">filename where compressed file will be saved</param>
        public void CompressStream(BinaryReader stream, string fileName)
        {
            var dictionary = new char[_dictionarySize];
            var buffer = new char[_bufferSize];

            var outputFile = File.Create(fileName + ".lz77");
            var outputStream = new BinaryWriter(outputFile);

            var fst = stream.ReadChars(_bufferSize);
            Array.Copy(fst, buffer, fst.Length);
            _bufferFillNumber = (ushort)fst.Length;

            while (_bufferFillNumber != 0)
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
                if(coderOut.Lenght < _bufferFillNumber)
                {
                    dictionary = ArrayExtension.ShiftElements(dictionary, (coderOut.Lenght + 1), ShiftDirection.Right);
                    Array.Copy(buffer, 0, dictionary, 0, coderOut.Lenght + 1);
                    buffer = ArrayExtension.ShiftElements(buffer, (coderOut.Lenght + 1), ShiftDirection.Left);

                    _bufferFillNumber -= (ushort)(coderOut.Lenght + 1);

                    var tmp = stream.ReadChars(coderOut.Lenght + 1);
                    if (tmp.Length != 0)
                    {
                        Array.Copy(tmp, 0, buffer, (_bufferSize - coderOut.Lenght - 1), (coderOut.Lenght + 1));
                        _bufferFillNumber += (ushort)(tmp.Length);
                    }

                    if (_type == Lz77SizeType.D1_B64)
                    {
                        var number = ConvertCoderOutputToShort(coderOut);
                        outputStream.Write(number);
                        outputStream.Write(coderOut.Character);
                    }
                    else
                    {
                        var number = ConvertCoderOutputToInt(coderOut);
                        outputStream.Write(number);
                        outputStream.Write(coderOut.Character);
                    }
                }
                else
                {
                    if (_type == Lz77SizeType.D1_B64)
                    {
                        var number = ConvertCoderOutputToShort(coderOut);
                        outputStream.Write(number);
                        outputStream.Write(coderOut.Character);
                    }
                    else
                    {
                        var number = ConvertCoderOutputToInt(coderOut);
                        outputStream.Write(number);
                        outputStream.Write(coderOut.Character);
                    }
                }

            } 

            outputStream.Flush();
            outputStream.Close();
            outputFile.Close();
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
