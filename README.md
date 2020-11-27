# LZ77.NET

.NET Implementation of LZ77 Algorithm

## How to build
Download nuget package: LZ77.NET  
Add in your project
```csharp 
  using LZ77; 
```

## Example of usage in C#
```csharp
  // Compress and decompress file
  int main(string[] args)
  {
    ICompressor compressor = new Lz77Compressor(Lz77BufferSize.B64);
    compressor.CompressFile("inputFileName.txt");
    compressor.DecompressFile("inputCompressedFile.txt.lz77", "outputFile.txt");
  }
```
