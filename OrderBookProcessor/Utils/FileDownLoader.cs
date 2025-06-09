
using System.Collections.Concurrent;
namespace OrderBookProcessor.Utils
{
    internal static class FileDownLoader
    {
        public static async Task DownloadFileInChunksAsync(string url, string outputPath, long chunkSize = 8 * 1024, int maxConcurrentDownloads = 4, int timeout = 30)
        {
            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);

            // 1. Get File Size
            long fileSize = await GetFileSizeAsync(client, url);
            if (fileSize <= 0)
            {
                Console.WriteLine("Failed to Get File size.");
                return;
            }
            // 2. If File Size < Chunk Size, Download it directly.
            if (fileSize <= chunkSize)
            {
                byte[] data = await DownloadChunkAsync(client, url, 0, fileSize - 1);
                await File.WriteAllBytesAsync(outputPath, data);
                return;
            }
            // 3.  Calculate Chunks
            var chunks = CalculateChunks(fileSize, chunkSize);
            var downloadedChunks = new ConcurrentDictionary<int, byte[]>();

            // 4. Download Chunks concurrently
            var tasks = new Task[maxConcurrentDownloads];
            int chunkIndex = 0;

            while (chunkIndex < chunks.Count())
            {
                for (int i = 0; i < maxConcurrentDownloads && chunkIndex < chunks.Count(); i++)
                {
                    int currentIndex = chunkIndex++;
                    var chunk = chunks[currentIndex];

                    tasks[i] = Task.Run(async () =>
                    {
                        byte[] data = await DownloadChunkAsync(client, url, chunk.Start, chunk.End);
                        downloadedChunks[currentIndex] = data;
                    });
                }

                await Task.WhenAll(tasks);
            }

            // 5. Merge files
            await MergeChunksAsync(downloadedChunks, outputPath, chunks.Count());
            LogUtils.logInfo($"Downloaded file to {outputPath}");
        }

        // Get File Size
        static async Task<long> GetFileSizeAsync(HttpClient client, string url)
        {
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
            response.EnsureSuccessStatusCode();
            return response.Content.Headers.ContentLength ?? 0;
        }

        // Calculate Chunks
        static (long Start, long End)[] CalculateChunks(long fileSize, long chunkSize)
        {
            int chunkCount = (int)Math.Ceiling((double)fileSize / chunkSize);
            var chunks = new (long Start, long End)[chunkCount];

            for (int i = 0; i < chunkCount; i++)
            {
                long start = i * chunkSize;
                long end = Math.Min(start + chunkSize - 1, fileSize - 1);
                chunks[i] = (start, end);
            }

            return chunks;
        }

        // Download Single Chunk
        static async Task<byte[]> DownloadChunkAsync(HttpClient client, string url, long start, long end)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        // Merge the Chunk to File
        static async Task MergeChunksAsync(ConcurrentDictionary<int, byte[]> chunks, string outputPath, int chunkCount)
        {
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            for (int i = 0; i < chunkCount; i++)
            {
                if (chunks.TryGetValue(i, out var data))
                {
                    await outputStream.WriteAsync(data, 0, data.Length);
                }
                else
                {
                    throw new Exception($"Chunk {i} is missing");
                }
            }
        }
    }
}
