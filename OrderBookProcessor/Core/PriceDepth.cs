using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.Core
{
    internal class PriceDepth
    {
        public string Symbol { get; }
        public List<PriceLevel> Bids { get; }
        public List<PriceLevel> Asks { get; }

        public PriceDepth(string symbol, List<PriceLevel> bids, List<PriceLevel> asks)
        {
            Symbol = symbol;
            Bids = bids;
            Asks = asks;
        }

        public string ToOutputString(uint sequenceNo)
        {
            string bidsStr = "[" + string.Join(", ", Bids.Select(b => b.ToString())) + "]";
            string asksStr = "[" + string.Join(", ", Asks.Select(a => a.ToString())) + "]";
            return $"{sequenceNo}, {Symbol}, {bidsStr}, {asksStr}";
        }

        public bool Equals(PriceDepth other)
        {
            if (other == null || Symbol != other.Symbol)
                return false;

            if (Bids.Count != other.Bids.Count || Asks.Count != other.Asks.Count)
                return false;

            for (int i = 0; i < Bids.Count; i++)
            {
                if (!Bids[i].Equals(other.Bids[i]))
                    return false;
            }

            for (int i = 0; i < Asks.Count; i++)
            {
                if (!Asks[i].Equals(other.Asks[i]))
                    return false;
            }

            return true;
        }
    }
}
