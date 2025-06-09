using System;

using System.Text;
using OrderBookProcessor.Utils;

namespace OrderBookProcessor.Core
{
    // Process the orders in parallel
    internal class OrderBookSerialExecutor : OrderBookExecutor
    {
        private Dictionary<string, Dictionary<char, Dictionary<long, Order>>> orderBooks; // symbol -> side -> orderId -> order
        private Dictionary<string, long> lastSequenceNumbers; // Track last sequence number per symbol for change detection
        private int depthLevels; // Number of levels to display (N)
        private Dictionary<string, List<string>> priceDepthSnapshot = new Dictionary<string, List<string>>();
        private const int MaxMessageSize = 1024;
        public OrderBookSerialExecutor(int levels)
        {
            orderBooks = new Dictionary<string, Dictionary<char, Dictionary<long, Order>>>();
            lastSequenceNumbers = new Dictionary<string, long>();
            depthLevels = levels;
            priceDepthSnapshot[BusinessConstant.All] = new List<string>();
        }

        public async override Task<Dictionary<string, List<string>>> CalculatePriceDepthSanpshotAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Read header (8 bytes: 4 for sequenceNo, 4 for messageSize)
                        byte[] headerBuffer = new byte[8];
                        int bytesRead = await reader.BaseStream.ReadAsync(headerBuffer, 0, 8, cancellationToken);
                        if (bytesRead == 0)
                            break; // End of stream
                        if (bytesRead < 8)
                        {
                            LogUtils.logErrorWithConsole($"Warning: Incomplete header read, got {bytesRead} bytes, expected 8");
                            break;
                        }

                        using (var headerStream = new MemoryStream(headerBuffer))
                        using (var headerReader = new BinaryReader(headerStream))
                        {
                            uint sequenceNo = BitConverter.ToUInt32(headerBuffer, 0);
                            uint messageSize = BitConverter.ToUInt32(headerBuffer, 4);

                            // Validate message size
                            if (messageSize == 0 || messageSize > MaxMessageSize)
                            {
                                LogUtils.logErrorWithConsole($"Warning: Invalid message size {messageSize} at sequence {sequenceNo}, skipping");
                                continue;
                            }

                            // Read message type (1 byte)
                            byte[] messageTypeBuffer = new byte[1];
                            bytesRead = await stream.ReadAsync(messageTypeBuffer, 0, 1, cancellationToken);
                            if (bytesRead < 1)
                            {
                                LogUtils.logErrorWithConsole($"Warning: Failed to read message type at sequence {sequenceNo}");
                                break;
                            }

                            char messageType = (char)messageTypeBuffer[0];
                            if (!"AUDE".Contains(messageType))
                            {
                                LogUtils.logErrorWithConsole($"Warning: Invalid message type '{messageType}' at sequence {sequenceNo}, skipping");
                                // Attempt to skip the remaining message data
                                byte[] skipBuffer = new byte[messageSize - 1];
                                await stream.ReadAsync(skipBuffer, 0, skipBuffer.Length, cancellationToken);
                                continue;
                            }

                            // Read message data
                            byte[] messageData = new byte[messageSize - 1];
                            bytesRead = await stream.ReadAsync(messageData, 0, messageData.Length, cancellationToken);
                            if (bytesRead < messageData.Length)
                            {
                                LogUtils.logErrorWithConsole($"Warning: Incomplete message data at sequence {sequenceNo}, got {bytesRead} bytes, expected {messageData.Length}");
                                break;
                            }

                            // Extract symbol
                            string symbol = Encoding.ASCII.GetString(messageData, 0, Math.Min(3, messageData.Length)).Trim();
                            if (string.IsNullOrEmpty(symbol))
                            {
                                Console.Error.WriteLine($"Warning: Invalid symbol at sequence {sequenceNo}, skipping");
                                continue;
                            }
                            // Handle a piece of order
                            var message = new Message { SequenceNo = sequenceNo, MessageType = messageType, Data = messageData };
                            ProcessSymbolMessages(symbol, message, cancellationToken);
                        }
                    }
                    return priceDepthSnapshot;
                }
            }
            catch (Exception) {
                throw;
            }
                
        }

        private void ProcessSymbolMessages(string symbol, Message message, CancellationToken cancellationToken)
        {
            try
            {
                // Initialize order book for this symbol
                if (!orderBooks.ContainsKey(symbol))
                    orderBooks[symbol] = new Dictionary<char, Dictionary<long, Order>>
                        {
                            { BusinessConstant.Bid, new Dictionary<long, Order>() },
                            { BusinessConstant.Sell, new Dictionary<long, Order>() }
                        };

                using (MemoryStream stream = new MemoryStream(message.Data!))
                using (var reader = new BinaryReader(stream))
                {
                    switch (message.MessageType)
                    {
                        case BusinessConstant.AddOrder:
                            ProcessOrderAdded(reader, symbol, message.SequenceNo);
                            break;
                        case BusinessConstant.UpdateOrder:
                            ProcessOrderUpdated(reader, symbol, message.SequenceNo);
                            break;
                        case BusinessConstant.DeleteOrder:
                            ProcessOrderDeleted(reader, symbol, message.SequenceNo);
                            break;
                        case BusinessConstant.ExecuteOrder:
                            ProcessOrderExecuted(reader, symbol, message.SequenceNo);
                            break;
                        default:
                            LogUtils.logErrorWithConsole($"Unknown message type '{message.MessageType}' for symbol {symbol} at sequence {message.SequenceNo}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtils.logErrorWithConsole($"Error processing symbol {symbol}: {ex.Message}");
            }
        }

        private void ProcessOrderAdded(BinaryReader reader, string symbol, uint sequenceNo)
        {
            reader.ReadChars(3); // Symbol
            long orderId = reader.ReadInt64();
            char side = reader.ReadChar();
            reader.ReadChars(3); // Reserved
            long size = reader.ReadInt64();
            int price = reader.ReadInt32();
            reader.ReadChars(4); // Reserved

            var sideBook = orderBooks[symbol][side];
            sideBook[orderId] = new Order { OrderId = orderId, Side = side, Volume = size, Price = price };
            OutputSnapshotIfChanged(symbol, sequenceNo);

        }

        private void ProcessOrderUpdated(BinaryReader reader, string symbol, uint sequenceNo)
        {
            reader.ReadChars(3); // Symbol
            long orderId = reader.ReadInt64();
            char side = reader.ReadChar();
            reader.ReadChars(3); // Reserved
            long size = reader.ReadInt64();
            int price = reader.ReadInt32();
            reader.ReadChars(4); // Reserved

            if (orderBooks[symbol][side].ContainsKey(orderId))
            {
                var order = orderBooks[symbol][side][orderId];
                order.Volume = size;
                order.Price = price;
            }

            OutputSnapshotIfChanged(symbol, sequenceNo);

        }

        private void ProcessOrderDeleted(BinaryReader reader, string symbol, uint sequenceNo)
        {
            reader.ReadChars(3); // Symbol
            long orderId = reader.ReadInt64();
            char side = reader.ReadChar();
            reader.ReadChars(3); // Reserved

            if (orderBooks[symbol][side].ContainsKey(orderId))
                orderBooks[symbol][side].Remove(orderId);

            OutputSnapshotIfChanged(symbol, sequenceNo);

        }

        private void ProcessOrderExecuted(BinaryReader reader, string symbol, uint sequenceNo)
        {
            reader.ReadChars(3); // Symbol
            long orderId = reader.ReadInt64();
            char side = reader.ReadChar();
            reader.ReadChars(3); // Reserved
            long tradedQuantity = reader.ReadInt64();


            if (orderBooks[symbol][side].ContainsKey(orderId))
            {
                var order = orderBooks[symbol][side][orderId];
                order.Volume -= tradedQuantity;
                if (order.Volume <= 0)
                    orderBooks[symbol][side].Remove(orderId);
            }

            OutputSnapshotIfChanged(symbol, sequenceNo);
        }

        private void OutputSnapshotIfChanged(string symbol, uint sequenceNo)
        {
            if (!orderBooks.ContainsKey(symbol))
                return;

            var bidBook = orderBooks[symbol][BusinessConstant.Bid];
            var askBook = orderBooks[symbol][BusinessConstant.Sell];

            // Aggregate by price
            var bidLevels = bidBook.Values
                .GroupBy(o => o.Price)
                .Select(g => new PriceLevel { Price = g.Key, TotalVolume = g.Sum(o => o.Volume) })
                .OrderByDescending(l => l.Price)
                .Take(depthLevels)
                .ToList();

            var askLevels = askBook.Values
                .GroupBy(o => o.Price)
                .Select(g => new PriceLevel { Price = g.Key, TotalVolume = g.Sum(o => o.Volume) })
                .OrderBy(l => l.Price)
                .Take(depthLevels)
                .ToList();

            // Check if snapshot has changed in the top N levels
            string newSnapshot = $"{sequenceNo}, {symbol}, [{string.Join(", ", bidLevels.Select(l => $"({l.Price}, {l.TotalVolume})"))}], [{string.Join(", ", askLevels.Select(l => $"({l.Price}, {l.TotalVolume})"))}]";

            if (!lastSequenceNumbers.ContainsKey(symbol) || GetSnapshot(symbol) != newSnapshot)
            {
                Console.WriteLine(newSnapshot);
                lastSequenceNumbers[symbol] = sequenceNo;
                LogSnapshot(newSnapshot, symbol, ref priceDepthSnapshot);
            }
        }

        private string GetSnapshot(string symbol)
        {
            var bidBook = orderBooks[symbol][BusinessConstant.Bid];
            var askBook = orderBooks[symbol][BusinessConstant.Sell];

            var bidLevels = bidBook.Values
                .GroupBy(o => o.Price)
                .Select(g => new PriceLevel { Price = g.Key, TotalVolume = g.Sum(o => o.Volume) })
                .OrderByDescending(l => l.Price)
                .Take(depthLevels)
                .ToList();

            var askLevels = askBook.Values
                .GroupBy(o => o.Price)
                .Select(g => new PriceLevel { Price = g.Key, TotalVolume = g.Sum(o => o.Volume) })
                .OrderBy(l => l.Price)
                .Take(depthLevels)
                .ToList();

            return $"{lastSequenceNumbers.GetValueOrDefault(symbol)},{symbol},[{string.Join(", ", bidLevels.Select(l => $"({l.Price}, {l.TotalVolume})"))}],[{string.Join(", ", askLevels.Select(l => $"({l.Price}, {l.TotalVolume})"))}]";
        }

    }
}
