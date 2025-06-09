using OrderBookProcessor.Core;
using OrderBookProcessor.Utils;
using System.Configuration;

namespace OrderBookProcessor.TaskManagement
{
    // Handle the stream file from internet
    internal class NetworkTaskWorker : BaseTaskWorker
    {
        int Timeout {  get; init; }
        int ChunkSize { get; init; }
        int MaxThread { get; init; }

        string FileURL { get; init; }

        public NetworkTaskWorker(int depthLevels, string url)
        {
            this.TimeStamp = this.getTimeStamp();
            this.DepthLevels = depthLevels;
            this.FileURL = url;
            this.setOrderBookExecutor(new OrderBookParallelExecutor(this.DepthLevels));

            // Load Network settings such as timeout, Download Chunk Size and Max download thread count
            if (!int.TryParse(ConfigurationManager.AppSettings[BusinessConstant.KEY_DOWNLOAD_TIMEOUT], out int timeout))
            {
                this.Timeout = BusinessConstant.DownLoadTimeout;
            }
            else
            {
                this.Timeout = timeout;
            }
            if (!int.TryParse(ConfigurationManager.AppSettings["DownLoadChunkSize"], out int chunkSize))
            {
                this.ChunkSize = BusinessConstant.DownLoadChunkSize;
            }
            else
            {
                this.ChunkSize = chunkSize;
            }
            if (!int.TryParse(ConfigurationManager.AppSettings["DownLoadMaxThread"], out int maxThread))
            {
                this.MaxThread = BusinessConstant.DownLoadMaxThread;
            }
            else
            {
                this.MaxThread = maxThread;
            }
        }

        public override async Task ExecuteTask()
        {
            LogStartInfo(nameof(NetworkTaskWorker));
            string fileName = this.TimeStamp + ConfigurationManager.AppSettings["DownLoadTempFile"];
            string downLoadFilePath = Path.Join(Directory.GetCurrentDirectory(), fileName);
            // Download File
            await FileDownLoader.DownloadFileInChunksAsync(this.FileURL, downLoadFilePath, this.ChunkSize, this.MaxThread, this.Timeout);
            // Calculate Depth Price
            Dictionary<string, List<string>>? result = null;
            using (var cts = new CancellationTokenSource())
            using (var fileStream = new FileStream(downLoadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                result = await this.Executor!.CalculatePriceDepthSanpshotAsync(fileStream, cts.Token);
            }
            FileUtils.DeleteFile(downLoadFilePath);
            await this.OutputPriceDepthSnapshot(result, this.TimeStamp);
        }

        public override void LogStartInfo(string taskType)
        {
            base.LogStartInfo(taskType);
            LogUtils.logInfo($"URL is {this.FileURL}");
        } 
    }
}
