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
        //private readonly ushort _dictionaryBitLen;

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
        private Lz77CoderOutputModel? GetLongestSubstring(char[] dictionary, char[] buffer)
        {
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

            //_dictionaryBitLen = 10;
            _bufferBitLen = 6;     
        }

        /// <summary>
        /// Compress input stream and save into new file with .lz77 extension
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

            while (_bufferFillNumber > 0)
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
                var coderOut = GetLongestSubstring(dictionary, buffer);
                if(coderOut.Length < _bufferFillNumber)
                {
                    //3
                    if ((_dictionaryFillNumber + coderOut.Length + 1) > _dictionarySize)
                    {
                        var rest = (ushort)((coderOut.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                        dictionary = ArrayExtension.ShiftElements(dictionary, rest, _dictionaryFillNumber, ShiftDirection.Left);
                        _dictionaryFillNumber -= rest;
                    }
                    //4

                    /*
                    char[] reverse = new char[coderOut.Length + 1];
                    Array.Copy(buffer, 0, reverse, 0, coderOut.Length + 1);
                    Array.Reverse(reverse);
                    */

                    Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, coderOut.Length + 1);
                    //5
                    buffer = ArrayExtension.ShiftElements(buffer, (coderOut.Length + 1), _bufferFillNumber, ShiftDirection.Left);

                    _bufferFillNumber -= (ushort)(coderOut.Length + 1);
                    _dictionaryFillNumber += (ushort)(coderOut.Length + 1);

                    //6
                    var tmp = stream.ReadChars(coderOut.Length + 1);
                    if (tmp.Length != 0)
                    {
                        Array.Copy(tmp, 0, buffer, (_bufferSize - coderOut.Length - 1), tmp.Length);
                        _bufferFillNumber += (ushort)(tmp.Length);
                    }
                    //7
                    var number = ConvertCoderOutputToShort(coderOut);
                    outputStream.Write(number);
                    outputStream.Write(coderOut.Character);                   
                }
                else
                {
                    _bufferFillNumber = (ushort)Math.Max(_bufferFillNumber - coderOut.Length, 0);
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
        /// Compress input stream and save into new file with .lz77 extension
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to compress</param>
        /// <param name="fileName">filename where compressed file will be saved</param>
        public void CompressFile(string fileName)
        {
            var dictionary = new char[_dictionarySize];
            var buffer = new char[_bufferSize];

            if(fileName.EndsWith(".txt"))
            {
                var inputFile = File.OpenRead(fileName);
                var outputFile = File.Create(fileName + ".lz77");

                var inputStream = new BinaryReader(inputFile);
                var outputStream = new BinaryWriter(outputFile);

                ushort _dictionaryFillNumber = 0;
                ushort _bufferFillNumber;

                var fst = inputStream.ReadChars(_bufferSize);
                Array.Copy(fst, buffer, fst.Length);
                _bufferFillNumber = (ushort)fst.Length;

                while (_bufferFillNumber > 0)
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
                    var coderOut = GetLongestSubstring(dictionary, buffer);
                    if (coderOut.Length < _bufferFillNumber)
                    {
                        //3
                        if ((_dictionaryFillNumber + coderOut.Length + 1) > _dictionarySize)
                        {
                            var rest = (ushort)((coderOut.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                            dictionary = ArrayExtension.ShiftElements(dictionary, rest, _dictionaryFillNumber, ShiftDirection.Left);
                            _dictionaryFillNumber -= rest;
                        }
                        //4

                        /*
                        char[] reverse = new char[coderOut.Length + 1];
                        Array.Copy(buffer, 0, reverse, 0, coderOut.Length + 1);
                        Array.Reverse(reverse);
                        */

                        Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, coderOut.Length + 1);
                        //5
                        buffer = ArrayExtension.ShiftElements(buffer, (coderOut.Length + 1), _bufferFillNumber, ShiftDirection.Left);

                        _bufferFillNumber -= (ushort)(coderOut.Length + 1);
                        _dictionaryFillNumber += (ushort)(coderOut.Length + 1);

                        //6
                        var tmp = inputStream.ReadChars(coderOut.Length + 1);
                        if (tmp.Length != 0)
                        {
                            Array.Copy(tmp, 0, buffer, (_bufferSize - coderOut.Length - 1), tmp.Length);
                            _bufferFillNumber += (ushort)(tmp.Length);
                        }
                        //7
                        var number = ConvertCoderOutputToShort(coderOut);
                        outputStream.Write(number);
                        outputStream.Write(coderOut.Character);
                    }
                    else
                    {
                        _bufferFillNumber = (ushort)Math.Max(_bufferFillNumber - coderOut.Length, 0);
                        var number = ConvertCoderOutputToShort(coderOut);
                        outputStream.Write(number);
                        outputStream.Write(coderOut.Character);
                    }
                }
                //flush data and close files
                inputStream.Close();
                inputFile.Close();

                outputStream.Flush();
                outputStream.Close();
                outputFile.Close();
            }
            else
            {
                throw new Exception("wrong file extension (should ends with .txt)");
            }

        }

        /// <summary>
        /// Decompress input stream and save original data to new file
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

            while (stream.BaseStream.Position != stream.BaseStream.Length)
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
                buffer[model.Length] = model.Character;
                //4
                if ((_dictionaryFillNumber + model.Length + 1) > _dictionarySize)
                {
                    var rest = (ushort)((model.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                    dictionary = ArrayExtension.ShiftElements(dictionary, rest, _dictionaryFillNumber, ShiftDirection.Left);
                    _dictionaryFillNumber -= rest;
                }
                //5
                Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, model.Length + 1);
                _dictionaryFillNumber += (ushort)(model.Length + 1);

                //6
                outputStream.Write(new ArraySegment<char>(buffer, 0, model.Length + 1));
            }

            outputStream.Flush();
            outputStream.Close();
            outputFile.Close();
        }

        /// <summary>
        /// Decompress input stream and save original data to new file
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to decompress</param>
        /// <param name="fileName">filename where decompressed file will be saved</param>
        public void DecompressFile(string fileName, string? outputFileName = null)
        {
            var dictionary = new char[_dictionarySize];
            var buffer = new char[_bufferSize];

            if (fileName.EndsWith(".lz77"))
            {
                var inputFile = File.OpenRead(fileName);
                var inputStream = new BinaryReader(inputFile);

                var outputFile = File.Create(outputFileName ?? (fileName[0..^5]));
                var outputStream = new BinaryWriter(outputFile);

                ushort _dictionaryFillNumber = 0;

                ushort number;
                char character;
                Lz77CoderOutputModel model;

                while (inputStream.BaseStream.Position != inputStream.BaseStream.Length)
                {
                    //1. pobierz (P,C,'a')
                    //2. skopiuj na podstawie P i C z 'dictionary' do 'buffer'
                    //3. doklej do 'buffer' 'a'
                    //4. przesuń C + 1 elementów w prawo w 'dictionary'
                    //5. dodaj C + 1 nowych elementów ze 'buffer' do 'dictionary'
                    //6. dodaj do pliku wyjsciowego 'buffer'

                    //1
                    number = inputStream.ReadUInt16();
                    character = inputStream.ReadChar();
                    model = ConvertShortToCoderOutput(number);
                    model.Character = character;
                    //2
                    Array.Copy(dictionary, model.Position, buffer, 0, model.Length);
                    //3
                    buffer[model.Length] = model.Character;
                    //4
                    if ((_dictionaryFillNumber + model.Length + 1) > _dictionarySize)
                    {
                        var rest = (ushort)((model.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                        dictionary = ArrayExtension.ShiftElements(dictionary, rest, _dictionaryFillNumber, ShiftDirection.Left);
                        _dictionaryFillNumber -= rest;
                    }
                    //5
                    Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, model.Length + 1);
                    _dictionaryFillNumber += (ushort)(model.Length + 1);

                    //6
                    outputStream.Write(new ArraySegment<char>(buffer, 0, model.Length + 1));
                }

                //flush data and close file
                inputStream.Close();
                inputFile.Close();

                outputStream.Flush();
                outputStream.Close();
                outputFile.Close();
            }
            else
            {
                throw new Exception("wrong input file extension (should end with .lz77)");
            }
            
        }

    }
}
