using LZ77.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LZ77.Models;
using System.Threading.Tasks;


namespace LZ77.Algorithms
{
    public enum Lz77BufferSize
    {
        B32 = 31,
        B64 = 63,
        B128 = 127,
        B256 = 255
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
        private Lz77CoderOutputModel ConvertShortToCoderOutput(ushort number)
        {
            return new Lz77CoderOutputModel 
            {
                Length = (byte)(number & _bufferSize),
                Position = (ushort)((number >> (_bufferBitLen)) & _dictionarySize)
            };
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
        /// Finds the longest matching pattern from <paramref name="buffer"/> inside <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="buffer"></param>
        /// <returns>should return Lenght smaller than '_bufferSize' </returns>
        private Lz77CoderOutputModel GetLongestSubstring(ReadOnlySpan<char> dictionary, ReadOnlySpan<char> buffer)
        {
            return KMPSearch.KMPGetLongestMatch(dictionary, buffer);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="type">Size of dictionary and buffer</param>
        public Lz77Compressor(Lz77BufferSize bufferSize = Lz77BufferSize.B64)
        {
            _dictionarySize = 32767;
            _bufferSize = (ushort)bufferSize;
            //_dictionaryBitLen = 10;
            _bufferBitLen = 8;     
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
                        dictionary = ArrayExtension.ShiftElements(dictionary, rest, _dictionaryFillNumber);
                        _dictionaryFillNumber -= rest;
                    }
                    //4
                    Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, coderOut.Length + 1);
                    //5
                    buffer = ArrayExtension.ShiftElements(buffer, (coderOut.Length + 1), _bufferFillNumber);

                    _bufferFillNumber -= (ushort)(coderOut.Length + 1);
                    _dictionaryFillNumber += (ushort)(coderOut.Length + 1);

                    //6
                    var tmp = stream.ReadChars(coderOut.Length + 1);
                    if (tmp.Length != 0)
                    {
                        //Array.Copy(tmp, 0, buffer, (_bufferSize - coderOut.Length - 1), tmp.Length);
                        tmp.CopyTo(buffer, (_bufferSize - coderOut.Length - 1));
                        _bufferFillNumber += (ushort)(tmp.Length);
                    }
                    //7
                    outputStream.Write(coderOut);                 
                }
                else
                {
                    _bufferFillNumber = (ushort)Math.Max(_bufferFillNumber - coderOut.Length, 0);
                    outputStream.Write(coderOut);
                }
            } 

            outputStream.Flush();
            outputStream.Close();
            outputFile.Close();
        }

        /// <summary>
        /// Compress input stream and save into new file with .lz77 extension
        /// </summary>
        /// <param name="fileName">filename where compressed file will be saved</param>
        public void CompressFile(string fileName)
        {
            var dictionary = new char[2 * _dictionarySize];
            var buffer = new char[2 * _bufferSize];

            if(fileName.EndsWith(".txt"))
            {
                var inputFile = File.OpenRead(fileName);
                var outputFile = File.Create(fileName + ".lz77");

                var inputStream = new BinaryReader(inputFile);
                var outputStream = new BinaryWriter(outputFile);

                ushort _dictionaryFillNumber = 0;
                ushort _bufferFillNumber;

                ushort _bufferSegmentOffset = 0;
                ushort _dictionarySegmentOffset = 0;

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
                    var coderOut = GetLongestSubstring( 
                        new ReadOnlySpan<char>(dictionary).Slice(_dictionarySegmentOffset, _dictionarySize), 
                        new ReadOnlySpan<char>(buffer).Slice(_bufferSegmentOffset, _bufferSize));
                    if (coderOut.Length < _bufferFillNumber)
                    {
                        //3
                        if ((_dictionaryFillNumber + coderOut.Length + 1) > _dictionarySize)
                        {
                            if ((_dictionarySegmentOffset + coderOut.Length + 1) >= _dictionarySize)
                            {  
                                dictionary = ArrayExtension.ShiftElements(dictionary, _dictionarySegmentOffset, _dictionarySize);
                                _dictionarySegmentOffset = 0;
                            }
                            var rest = (ushort)((coderOut.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                            _dictionarySegmentOffset += rest;
                            _dictionaryFillNumber -= rest;
                        }   
                        //4
                        Array.Copy(buffer, _bufferSegmentOffset, dictionary, _dictionarySegmentOffset + _dictionaryFillNumber, coderOut.Length + 1);
                        //5
                        if(_bufferSegmentOffset + (coderOut.Length + 1) >= _bufferSize) 
                        {
                            buffer = ArrayExtension.ShiftElements(buffer, _bufferSegmentOffset, _bufferSize);
                            _bufferSegmentOffset = 0;
                        }

                        _bufferFillNumber -= (ushort)(coderOut.Length + 1);
                        _bufferSegmentOffset += (ushort)(coderOut.Length + 1);
                        _dictionaryFillNumber += (ushort)(coderOut.Length + 1);

                        //6
                        var tmp = inputStream.ReadChars(coderOut.Length + 1);
                        if (tmp.Length != 0)
                        {
                            //Array.Copy(tmp, 0, buffer, (_bufferSize - coderOut.Length - 1), tmp.Length);
                            tmp.CopyTo(buffer, (_bufferSegmentOffset + _bufferSize - coderOut.Length - 1));
                            _bufferFillNumber += (ushort)(tmp.Length);
                        }
                        //7
                        outputStream.Write(coderOut);
                        //Console.Write($"{coderOut.Character}");
                    }
                    else
                    {
                        _bufferFillNumber = (ushort)Math.Max(_bufferFillNumber - coderOut.Length, 0);
                        outputStream.Write(coderOut);
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
        /// Compress input stream and save into new file with .lz77 extension
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to compress</param>
        /// <param name="fileName">filename where compressed file will be saved</param>
        public MemoryStream CompressStreamToMemory(BinaryReader stream)
        {
            var dictionary = new char[_dictionarySize];
            var buffer = new char[_bufferSize];

            var memory = new MemoryStream();
            var outputStream = new BinaryWriter(memory);

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
                if (coderOut.Length < _bufferFillNumber)
                {
                    //3
                    if ((_dictionaryFillNumber + coderOut.Length + 1) > _dictionarySize)
                    {
                        var rest = (ushort)((coderOut.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                        dictionary = ArrayExtension.ShiftElements(dictionary, rest, _dictionaryFillNumber);
                        _dictionaryFillNumber -= rest;
                    }
                    //4
                    Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, coderOut.Length + 1);
                    //5
                    buffer = ArrayExtension.ShiftElements(buffer, (coderOut.Length + 1), _bufferFillNumber);

                    _bufferFillNumber -= (ushort)(coderOut.Length + 1);
                    _dictionaryFillNumber += (ushort)(coderOut.Length + 1);

                    //6
                    var tmp = stream.ReadChars(coderOut.Length + 1);
                    if (tmp.Length != 0)
                    {
                        //Array.Copy(tmp, 0, buffer, (_bufferSize - coderOut.Length - 1), tmp.Length);
                        tmp.CopyTo(buffer, (_bufferSize - coderOut.Length - 1));
                        _bufferFillNumber += (ushort)(tmp.Length);
                    }
                    //7
                    outputStream.Write(coderOut);
                }
                else
                {
                    _bufferFillNumber = (ushort)Math.Max(_bufferFillNumber - coderOut.Length, 0);
                    outputStream.Write(coderOut);
                }
            }

            return memory;
        }

        /// <summary>
        /// Decompress input stream and save original data to new file
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to decompress</param>
        /// <param name="fileName">filename where decompressed file will be saved</param>
        public void DecompressStream(BinaryReader stream, string fileName)
        {
            var dictionary = new char[2 * _dictionarySize];
            var buffer = new char[_bufferSize];

            var outputFile = File.Create(fileName + ".txt");
            var outputStream = new BinaryWriter(outputFile);

            ushort _dictionaryFillNumber = 0;

            ushort _dictionarySegmentOffset = 0;

            while (stream.BaseStream.Position != stream.BaseStream.Length)
            {
                //1. pobierz (P,C,'a')
                //2. skopiuj na podstawie P i C z 'dictionary' do 'buffer'
                //3. doklej do 'buffer' 'a'
                //4. przesuń C + 1 elementów w prawo w 'dictionary'
                //5. dodaj C + 1 nowych elementów ze 'buffer' do 'dictionary'
                //6. dodaj do pliku wyjsciowego 'buffer'

                //1
                var model = new Lz77CoderOutputModel
                {
                    Position = stream.ReadUInt16(),
                    Length = stream.ReadByte(),
                    Character = stream.ReadChar()
                };
                //2
                Array.Copy(dictionary, model.Position, buffer, 0, model.Length);
                //3
                buffer[model.Length] = model.Character;
                //4
                if ((_dictionaryFillNumber + model.Length + 1) > _dictionarySize)
                {
                    if ((_dictionarySegmentOffset + model.Length + 1) >= _dictionarySize)
                    {  
                        dictionary = ArrayExtension.ShiftElements(dictionary, _dictionarySegmentOffset, _dictionarySize);
                        _dictionarySegmentOffset = 0;
                    }
                    var rest = (ushort)((model.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                    _dictionarySegmentOffset += rest;
                    _dictionaryFillNumber -= rest;
                }
                //5
                Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, model.Length + 1);
                _dictionaryFillNumber += (ushort)(model.Length + 1);

                //6
                var span = new ReadOnlySpan<char>(buffer);
                outputStream.Write(span.Slice(0, model.Length + 1));
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
        public void DecompressFile(string fileName, string? outputFileName)
        {
            var dictionary = new char[2 * _dictionarySize];
            var buffer = new char[_bufferSize];

            if (fileName.EndsWith(".lz77"))
            {
                var inputFile = File.OpenRead(fileName);
                var inputStream = new BinaryReader(inputFile);

                var outputFile = File.Create(outputFileName ?? (fileName[0..^5]));
                var outputStream = new BinaryWriter(outputFile);

                ushort _dictionaryFillNumber = 0;

                ushort _dictionarySegmentOffset = 0;

                while (inputStream.BaseStream.Position != inputStream.BaseStream.Length)
                {
                    //1. pobierz (P,C,'a')
                    //2. skopiuj na podstawie P i C z 'dictionary' do 'buffer'
                    //3. doklej do 'buffer' 'a'
                    //4. przesuń C + 1 elementów w prawo w 'dictionary'
                    //5. dodaj C + 1 nowych elementów ze 'buffer' do 'dictionary'
                    //6. dodaj do pliku wyjsciowego 'buffer'

                    //1
                    var model = new Lz77CoderOutputModel
                    {
                        Position = inputStream.ReadUInt16(),
                        Length = inputStream.ReadByte(),
                        Character = inputStream.ReadChar()
                    };
                    //2
                    //Array.Copy(dictionary, _dictionarySegmentOffset + model.Position, buffer, 0, model.Length);
                    ReadOnlySpan<char> source = new (dictionary, _dictionarySegmentOffset + model.Position, model.Length);
                    //3
                    //buffer[model.Length] = model.Character;
                    //4
                    if(_dictionaryFillNumber > 32700) 
                        _dictionarySegmentOffset += 0;
                    if ((_dictionaryFillNumber + model.Length + 1) > _dictionarySize)
                    {
                        if ((_dictionarySegmentOffset + model.Length + 1) >= _dictionarySize)
                        {  
                            dictionary = ArrayExtension.ShiftElements(dictionary, _dictionarySegmentOffset, _dictionarySize);
                            _dictionarySegmentOffset = 0;
                        }
                        var rest = (ushort)((model.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                        _dictionarySegmentOffset += rest;
                        _dictionaryFillNumber -= rest;
                    }
                    //5
                    //Array.Copy(buffer, 0, dictionary, _dictionarySegmentOffset + _dictionaryFillNumber, model.Length + 1);
                    Span<char> dest = new (dictionary, _dictionarySegmentOffset + _dictionaryFillNumber, model.Length + 1);
                    source.CopyTo(dest);
                    dest[model.Length] = model.Character;
                    _dictionaryFillNumber += (ushort)(model.Length + 1);
                    //6
                    //var span = new ReadOnlySpan<char>(buffer);
                    outputStream.Write(dest);
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

        /// <summary>
        /// Decompress input stream and save original data to new file
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to decompress</param>
        /// <param name="fileName">filename where decompressed file will be saved</param>
        public MemoryStream DecompressStreamToMemory(BinaryReader stream)
        {
            var dictionary = new char[_dictionarySize];
            var buffer = new char[_bufferSize];

            var memory = new MemoryStream();
            var outputStream = new BinaryWriter(memory);

            ushort _dictionaryFillNumber = 0;

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
                model = new Lz77CoderOutputModel
                {
                    Position = stream.ReadUInt16(),
                    Length = stream.ReadByte(),
                    Character = stream.ReadChar()
                };
                //2
                Array.Copy(dictionary, model.Position, buffer, 0, model.Length);
                //3
                buffer[model.Length] = model.Character;
                //4
                if ((_dictionaryFillNumber + model.Length + 1) > _dictionarySize)
                {
                    var rest = (ushort)((model.Length + 1) - (_dictionarySize - _dictionaryFillNumber));
                    dictionary = ArrayExtension.ShiftElements(dictionary, rest, _dictionaryFillNumber);
                    _dictionaryFillNumber -= rest;
                }
                //5
                Array.Copy(buffer, 0, dictionary, _dictionaryFillNumber, model.Length + 1);
                _dictionaryFillNumber += (ushort)(model.Length + 1);

                //6
                var span = new ReadOnlySpan<char>(buffer);
                outputStream.Write(span.Slice(0, model.Length + 1));
            }
            return memory;
        }
    }
}
