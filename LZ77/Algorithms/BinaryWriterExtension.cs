using System.IO;

using LZ77.Models;

public static class BinaryWriterExtension
    {
        
        public static void Write(this BinaryWriter writer, Lz77CoderOutputModel model)
        {
            writer.Write(model.Position);
            writer.Write(model.Length);
            writer.Write(model.Character);
        }
    }