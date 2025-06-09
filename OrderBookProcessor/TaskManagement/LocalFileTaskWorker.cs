using OrderBookProcessor.Core;
using OrderBookProcessor.Utils;

namespace OrderBookProcessor.TaskManagement
{
    // Handle local stream file
    internal class LocalFileTaskWorker : BaseTaskWorker
    {
        string LocalStreamFilePath { get; init; }
        public LocalFileTaskWorker(int depthLevels, string localStreamFilePath)
        {
            this.TimeStamp = this.getTimeStamp();
            this.LocalStreamFilePath = localStreamFilePath;
            this.DepthLevels = depthLevels;
            this.setOrderBookExecutor(new OrderBookParallelExecutor(this.DepthLevels));
        }
        public override async Task ExecuteTask()
        {
            LogStartInfo(nameof(LocalFileTaskWorker));
            using (var cts = new CancellationTokenSource())
            using (var stream = new FileStream(this.LocalStreamFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var result = await this.Executor!.CalculatePriceDepthSanpshotAsync(stream, cts.Token);
                await this.OutputPriceDepthSnapshot(result, this.TimeStamp);
            }
        }

        public override void LogStartInfo(string taskType)
        {
            base.LogStartInfo(taskType);
            LogUtils.logInfo($"Local stream file path is {Path.GetFullPath(this.LocalStreamFilePath)}");
        }
    }
}
