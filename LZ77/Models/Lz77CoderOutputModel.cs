using System;
using System.Collections.Generic;
using System.Text;

namespace LZ77.Models
{
    public record Lz77CoderOutputModel
    {     
        public ushort Position { get; init; } = default!;
        public byte Length { get; init; } = default!;
        public char Character { get; init; } = default!;
    }
}
