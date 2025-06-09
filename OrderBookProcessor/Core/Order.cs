using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.Core
{
    internal class Order
    {
        public long OrderId { get; set; }
        public char Side { get; set; } // 'B' for Bid, 'S' for Ask
        public long Volume { get; set; }
        public int Price { get; set; }
    }
}
