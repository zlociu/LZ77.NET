using System;
using System.Collections.Generic;
using System.Text;

namespace LZ77.Models
{
    public record Lz77CoderOutputModel(ushort Position, byte Length, char Character);
}
