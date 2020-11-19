# LZ77.NET

.NET Implementation of LZ77 Algorithm

## How to build
Download nuget package: LZ77.NET

## Example of usage in C#
```csharp
  // Compressing text file
  int main(string[] args)
  {
    ICompressor compressor = new Lz77Compressor(Lz77SizeType.D1B64);
    BinarymReader stream = new BinaryReader(File.OpenRead("inputFileName.txt"));
    compressor.Compress(stream, "outputFileName");
  }
```
