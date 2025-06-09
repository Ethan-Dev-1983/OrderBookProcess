using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.Core
{
    internal interface IOrderBookExecutor
    {
        Task<Dictionary<string, List<string>>> CalculatePriceDepthSanpshotAsync(Stream inputStream, CancellationToken cancellationToken = default);
    }
}
