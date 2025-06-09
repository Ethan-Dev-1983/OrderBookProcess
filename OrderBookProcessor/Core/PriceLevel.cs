using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.Core
{
    internal class PriceLevel
    {
        public int Price { get; set; }
        public long TotalVolume { get; set; }
    }
}
