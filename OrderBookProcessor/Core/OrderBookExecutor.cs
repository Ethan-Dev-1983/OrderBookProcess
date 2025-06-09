using System.Collections.Concurrent;
using System.Text;
using OrderBookProcessor.Utils;

namespace OrderBookProcessor.Core
{
    internal abstract class OrderBookExecutor : IOrderBookExecutor
    {

        abstract public Task<Dictionary<string, List<string>>> CalculatePriceDepthSanpshotAsync(Stream stream, CancellationToken cancellationToken = default);
        public virtual void LogSnapshot(string snapShot, string symbol, ref Dictionary<string, List<string>> priceDepthSnapshot)
        {
            if (!priceDepthSnapshot.TryGetValue(symbol, out var symbolList))
            {
                symbolList = new List<string>();
            }

            symbolList?.Add(snapShot);
            priceDepthSnapshot[symbol] = symbolList!;
            priceDepthSnapshot[BusinessConstant.All].Add(snapShot);
        }
    }
}

