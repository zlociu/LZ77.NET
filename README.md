# LZ77.NET

C# Implementation of [LZ77](https://en.wikipedia.org/wiki/LZ77_and_LZ78) Algorithm.

### How to build
1. Go to folder with ___LZ77.csproj___ file.
2. Open CMD.
3. Type: `dotnet build -c Release`

### Features
 - compress text files
 - decompress ___*.lz77___ files
 - measure process time

### Usage examples (Windows)
 - Compress file and measure time:  
   `LZ77.exe -c <filename> -t` 
 - Decompress file:  
   `LZ77.exe -d <filename.lz77> -o <outputFileName>`
 - Print help:  
   `LZ77.exe -h`