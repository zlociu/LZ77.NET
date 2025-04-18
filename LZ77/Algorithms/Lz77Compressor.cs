using LZ77.Interfaces;
using LZ77.Models;
using System;
using System.IO;

namespace LZ77.Algorithms
{
    public enum Lz77BufferSize
    {
        B32 = 31,
        B64 = 63,
        B128 = 127,
        B256 = 255
    };

    // ------------< Compression algorithm >-------------
    //1. find longest substring in 'buffer'(from left) which exists in 'dictionary'
    //2. create (P,C,'a') where:
    //      P - index where substring starts in 'dictionary'
    //      C - substring length
    //      'a' - next sign in 'buffer' after substring 
    //3. move 'dictionary' (C + 1) positions right
    //4. copy C + 1 elements from buffer to 'dictionary'
    //5. move C + 1 elements left in 'buffer'
    //6. move C + 1 new elements from 'stream' to 'buffer'
    //7. add to output file C + (P << bitLen(C)) and 'a' as a number and sign

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="bufferSize">Size of buffer in bytes</param>
    public sealed class Lz77Compressor(Lz77BufferSize bufferSize = Lz77BufferSize.B64) : ICompressor
    {
        //arrays size 
        private readonly ushort _dictionarySize = 32767;
        private readonly ushort _bufferSize = (ushort)bufferSize;

        /// <summary>
        /// Finds the longest matching pattern from <paramref name="buffer"/> inside <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="buffer"></param>
        /// <returns>should return Lenght smaller than '_bufferSize' </returns>
        private static Lz77CoderOutputModel GetLongestSubstring(ReadOnlySpan<char> dictionary, ReadOnlySpan<char> buffer)
        {
            return KMPSearch.KMPGetLongestMatch(dictionary, buffer);
        }

        /// <summary>
        /// Compress input stream and save into new file with .lz77 extension
        /// </summary>
        /// <param name="fileName">file name of compressing file </param>
        /// <param name="outputFileName">file name where compressed file will be saved (without '.lz77' suffix)</param>
        public void CompressFile(string fileName, string? outputFileName)
        {
            Span<char> dictionary = new char[2 * _dictionarySize];
            Span<char> buffer = new char[4 * _bufferSize];

            var inputFile = File.OpenRead(fileName);
            var outputFile = File.Create((outputFileName ?? fileName) + ".lz77");

            var inputStream = new BinaryReader(inputFile);
            var outputStream = new BinaryWriter(outputFile);

            ushort _dictionaryFillNumber = 0;
            ushort _bufferFillNumber;

            ushort _bufferSegmentOffset = 0;
            ushort _dictionarySegmentOffset = 0;

            bool endData = false;

            var fst = inputStream.ReadChars(4 * _bufferSize);
            fst.CopyTo(buffer);

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
                    dictionary.Slice(_dictionarySegmentOffset, _dictionarySize),
                    buffer.Slice(_bufferSegmentOffset, _bufferSize));
                if (coderOut.Length < _bufferFillNumber)
                {
                    //3
                    var coderOutLengthPlusOne = coderOut.Length + 1;
                    if ((_dictionaryFillNumber + coderOutLengthPlusOne) > _dictionarySize)
                    {
                        if ((_dictionarySegmentOffset + coderOutLengthPlusOne) >= _dictionarySize)
                        {
                            Span<char> arr = new char[2 * _dictionarySize];
                            dictionary.Slice(_dictionarySegmentOffset, _dictionarySize).CopyTo(arr);
                            dictionary = arr;
                            _dictionarySegmentOffset = 0;
                        }
                        var rest = (ushort)(coderOutLengthPlusOne - (_dictionarySize - _dictionaryFillNumber));
                        _dictionarySegmentOffset += rest;
                        _dictionaryFillNumber -= rest;
                    }
                    //4
                    buffer
                        .Slice(_bufferSegmentOffset, coderOutLengthPlusOne)
                        .CopyTo(dictionary.Slice(_dictionarySegmentOffset + _dictionaryFillNumber, coderOutLengthPlusOne));

                    //5
                    if (_bufferSegmentOffset + coderOutLengthPlusOne >= (3 * _bufferSize))
                    {
                        Span<char> arr = new char[4 * _bufferSize];

                        // 6
                        if (!endData)
                        {
                            var tmp = inputStream.ReadChars(4 * _bufferSize - _bufferFillNumber);
                            tmp.CopyTo(arr[_bufferFillNumber..]);
                            endData = tmp.Length < (4 * _bufferSize - _bufferFillNumber);
                            _bufferFillNumber += (ushort)(tmp.Length);
                        }

                        buffer[_bufferSegmentOffset..].CopyTo(arr);
                        buffer = arr;
                        _bufferSegmentOffset = 0;
                    }

                    _bufferFillNumber -= (ushort)coderOutLengthPlusOne;
                    _bufferSegmentOffset += (ushort)coderOutLengthPlusOne;
                    _dictionaryFillNumber += (ushort)coderOutLengthPlusOne;

                    //7
                    outputStream.Write(coderOut);
                }
                else
                {
                    //6
                    _bufferFillNumber = 0;
                    //7
                    outputStream.Write(coderOut);
                }
            }
            //flush data and close files
            inputStream.Close();
            inputFile.Dispose();

            outputStream.Flush();
            outputStream.Close();
            outputFile.Dispose();
        }

        /// <summary>
        /// Decompress input stream and save original data to new file
        /// </summary>
        /// <param name="stream">binary stream, source of coderOut to decompress</param>
        /// <param name="fileName">filename where decompressed file will be saved</param>
        public void DecompressFile(string fileName, string? outputFileName)
        {
            Span<char> dictionary = new char[2 * _dictionarySize];

            if (fileName.EndsWith(".lz77"))
            {
                var inputFile = File.OpenRead(fileName);
                var inputStream = new BinaryReader(inputFile);

                var outputFile = File.Create(outputFileName ?? (fileName[0..^5]));
                var outputStream = new BinaryWriter(outputFile);

                ushort _dictionaryFillNumber = 0;
                ushort _dictionarySegmentOffset = 0;

                var maxValidPosition = inputStream.BaseStream.Length - 4;
                while (inputStream.BaseStream.Position < maxValidPosition)
                {
                    //1. pobierz (P,C,'a')
                    //2. skopiuj na podstawie P i C z 'dictionary' do 'buffer'
                    //3. doklej do 'buffer' 'a'
                    //4. przesuń C + 1 elementów w prawo w 'dictionary'
                    //5. dodaj C + 1 nowych elementów ze 'buffer' do 'dictionary'
                    //6. dodaj do pliku wyjsciowego 'buffer'

                    //1
                    var model = new Lz77CoderOutputModel(
                        inputStream.ReadUInt16(),
                        inputStream.ReadByte(),
                        inputStream.ReadChar());
                    //2
                    //3
                    ReadOnlySpan<char> source = dictionary.Slice(_dictionarySegmentOffset + model.Position, model.Length);
                    //4
                    var modelLengthPlusOne = model.Length + 1;
                    if ((_dictionaryFillNumber + modelLengthPlusOne) > _dictionarySize)
                    {
                        if ((_dictionarySegmentOffset + modelLengthPlusOne) >= _dictionarySize)
                        {
                            Span<char> arr = new char[2 * _dictionarySize];
                            dictionary.Slice(_dictionarySegmentOffset, _dictionarySize).CopyTo(arr);
                            dictionary = arr;
                            _dictionarySegmentOffset = 0;
                        }
                        var rest = (ushort)(modelLengthPlusOne - (_dictionarySize - _dictionaryFillNumber));
                        _dictionarySegmentOffset += rest;
                        _dictionaryFillNumber -= rest;
                    }
                    //5
                    Span<char> dest = dictionary.Slice(_dictionarySegmentOffset + _dictionaryFillNumber, modelLengthPlusOne);
                    source.CopyTo(dest);
                    dest[model.Length] = model.Character;
                    _dictionaryFillNumber += (ushort)(modelLengthPlusOne);
                    //6
                    outputStream.Write(dest);
                }
                //last iteration  
                var last = new Lz77CoderOutputModel(
                    Position: inputStream.ReadUInt16(),
                    Length: inputStream.ReadByte(),
                    Character: inputStream.ReadChar());
                outputStream.Write(dictionary.Slice(_dictionarySegmentOffset + last.Position, last.Length));

                //flush data and close file
                inputStream.Close();
                inputFile.Dispose();

                outputStream.Flush();
                outputStream.Close();
                outputFile.Dispose();
            }
            else
            {
                throw new FileNotFoundException("wrong input file extension (should end with .lz77)");
            }
        }

        static class KMPSearch
        {
            /// <summary>
            /// Bulds a table that allows the search algorithm to work.
            /// Use before using the search function each time the <paramref name="buffer"/> changes.
            /// </summary>
            /// <param name="buffer"></param>
            private static int[] BuildSearchTable(ReadOnlySpan<char> buffer)
            {
                int[] tab = new int[buffer.Length];

                int i = 2, j = 0;
                tab[0] = -1; tab[1] = 0;

                while (i < buffer.Length)
                {
                    if (buffer[i - 1] == buffer[j])
                    {
                        ++j;
                        tab[i] = j;
                        ++i;
                    }
                    else
                    {
                        if (j > 0)
                        {
                            j = tab[j];
                        }
                        else
                        {
                            tab[i] = 0;
                            ++i;
                        }
                    }
                }
                return tab;
            }

            /// <summary>
            /// Finds the longest matching pattern from <paramref name="buffer"/> inside <paramref name="dictionary"/>.
            /// </summary>
            /// <param name="dictionary"></param>
            /// <param name="buffer"></param>
            /// <returns>Lz77CoderOutputModel</returns>
            public static Lz77CoderOutputModel KMPGetLongestMatch(ReadOnlySpan<char> dictionary, ReadOnlySpan<char> buffer)
            {

                if (buffer.Length == 0) throw new IndexOutOfRangeException();

                var tab = BuildSearchTable(buffer);

                int m = 0;  // Beginning of the first fit in dictionary
                int i = 0;  // Position of the current char in buffer

                int bestPos = 0;
                int bestLength = 0;

                while (m + i < dictionary.Length)
                {
                    if (buffer[i] == dictionary[m + i])
                    {
                        ++i;

                        if (i == buffer.Length - 1)
                        {
                            return new Lz77CoderOutputModel(Position: (ushort)m, Length: (byte)i, Character: buffer[i]);
                        }

                        if (i > bestLength)
                        {
                            bestLength = i;
                            bestPos = m;
                        }
                    }
                    else
                    {
                        m = m + i - tab[i];
                        if (i > 0)
                        {
                            i = tab[i];
                        }
                    }
                }

                return new Lz77CoderOutputModel(
                    Position: (ushort)bestPos,
                    Length: (byte)bestLength,
                    Character: buffer[bestLength]);
            }
        }
    }
}
