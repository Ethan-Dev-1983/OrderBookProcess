using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.Core
{
    internal class Message
    {
        public uint SequenceNo { get; set; }
        public char MessageType { get; set; }
        public byte[]? Data { get; init; }
    }
}
