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

        //arrays size 
        private readonly ushort _dictionarySize;
        private readonly ushort _bufferSize;

        //how many bits is needed to write: Position and Lenght from LZ77 coder output 
        private readonly ushort _dictionaryBitLen;
        private readonly ushort _bufferBitLen;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private Lz77CoderOutputModel ConvertIntToCoderOutput(int number)
        {
            Lz77CoderOutputModel model = new Lz77CoderOutputModel
            {
                Length = (ushort)(number % (int)Math.Pow(2, _bufferBitLen)),
                Position = (ushort)((number >> (_bufferBitLen)) & _dictionarySize)
            };
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private Lz77CoderOutputModel ConvertShortToCoderOutput(ushort number)
        {
            Lz77CoderOutputModel model = new Lz77CoderOutputModel
            {
                Length = (ushort)(number & _bufferSize),
                Position = (ushort)((number >> (_bufferBitLen)) & _dictionarySize)
            };
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
            number = model.Length;
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
            number = model.Length;
            number += (model.Position << _bufferBitLen);
            return number;
        }


        /// <summary>
        /// Finds the longest matching pattern from <paramref name="buffer"/> inside <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="buffer"></param>
        /// <returns>should return Lenght smaller than '_bufferSize' </returns>
        private Lz77CoderOutputModel GetLongestSubstring(ref char[] dictionary, ref char[] buffer)
        {
            /*ushort len = 0, offset = 0;
            char sign = ' ';
            for(ushort i = 0; i < _bufferSize; i++)
            {
                len = i;
                if(i>2000) break;
            }
            var output = new Lz77CoderOutputModel()
            {
                Length = len,
                Position = offset,
                Character = sign
            };*/
            return KMPSearch.KMPGetLongestMatch(dictionary, buffer);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="type">Size of dictionary and buffer</param>
        public Lz77Compressor()
        {
            _dictionarySize = 1023;
            _bufferSize = 63;

            _dictionaryBitLen = 10;
            _bufferBitLen = 6;     
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

            ushort _dictionaryFillNumber = 0;
            ushort _bufferFillNumber;

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

                //1 2
                var coderOut = GetLongestSubstring(ref dictionary, ref buffer);
                if(coderOut.Length < _bufferFillNumber)
                {
                    //3
                    dictionary = ArrayExtension.ShiftElements(dictionary, (coderOut.Length + 1), _dictionaryFillNumber, ShiftDirection.Right);
                    //4
                    Array.Copy(buffer, 0, dictionary, 0, coderOut.Length + 1);
                    //5
                    buffer = ArrayExtension.ShiftElements(buffer, (coderOut.Length + 1), _dictionaryFillNumber, ShiftDirection.Left);

                    _bufferFillNumber -= (ushort)(coderOut.Length + 1);
                    _dictionaryFillNumber = (ushort)Math.Min(_dictionaryFillNumber + (coderOut.Length + 1), _dictionarySize);

                    //6
                    var tmp = stream.ReadChars(coderOut.Length + 1);
                    if (tmp.Length != 0)
                    {
                        Array.Copy(tmp, 0, buffer, (_bufferSize - coderOut.Length - 1), (coderOut.Length + 1));
                        _bufferFillNumber += (ushort)(tmp.Length);
                    }
                    //7
                    var number = ConvertCoderOutputToShort(coderOut);
                    outputStream.Write(number);
                    outputStream.Write(coderOut.Character);                   
                }
                else
                {
                    var number = ConvertCoderOutputToShort(coderOut);
                    outputStream.Write(number);
                    outputStream.Write(coderOut.Character);                    
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
            var dictionary = new char[_dictionarySize];
            var buffer = new char[_bufferSize];

            var outputFile = File.Create(fileName + ".txt");
            var outputStream = new BinaryWriter(outputFile);

            ushort _dictionaryFillNumber = 0;

            ushort number;
            char character;
            Lz77CoderOutputModel model;

            while (stream.PeekChar() != -1)
            {
                //1. pobierz (P,C,'a')
                //2. skopiuj na podstawie P i C z 'dictionary' do 'buffer'
                //3. doklej do 'buffer' 'a'
                //4. przesuń C + 1 elementów w prawo w 'dictionary'
                //5. dodaj C + 1 nowych elementów ze 'buffer' do 'dictionary'
                //6. dodaj do pliku wyjsciowego 'buffer'

                //1
                number = stream.ReadUInt16();
                character = stream.ReadChar();
                model = ConvertShortToCoderOutput(number);
                model.Character = character;
                //2
                Array.Copy(dictionary, model.Position, buffer, 0, model.Length);
                //3
                buffer[model.Length + 1] = model.Character;
                //4
                dictionary = ShiftElements(dictionary, model.Length + 1, _dictionaryFillNumber, ShiftDirection.Right);
                //5
                Array.Copy(buffer, 0, dictionary, 0, model.Length + 1);

                _dictionaryFillNumber += (ushort)(model.Length + 1);

                //6
                outputStream.Write(new ArraySegment<char>(buffer, 0, model.Length + 1));

            }

            outputStream.Flush();
            outputStream.Close();
            outputFile.Close();
        }

    }
}
