using System;
using System.Collections.Generic;
using System.Text;

namespace LZ77.Models
{
    class Lz77CoderOutputModel
    {     
        public ushort Position { get; set; }
        public ushort Length { get; set; }
        public char Character { get; set; }

        public Lz77CoderOutputModel()
        {
            Position = 0;
            Length = 0;
            Character = ' ';
        }
    }
}
