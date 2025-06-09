using OrderBookProcessor.Core;
using OrderBookProcessor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.TaskManagement
{
    // Hanlde StdIn stream to pipe
    internal class LocalStdInTaskWorker:BaseTaskWorker
    {
        public LocalStdInTaskWorker(int depthLevels)
        {
            this.DepthLevels = depthLevels;
            this.setOrderBookExecutor(new OrderBookSerialExecutor(depthLevels));
            this.TimeStamp = this.getTimeStamp();
        }
        public override async Task ExecuteTask()
        {
            LogStartInfo(nameof(LocalStdInTaskWorker));
            if (!CheckShell()) {
                 throw new Exception("Pipe mode can only be supported in Bash Shell. Kindly run OrderBookProcessor <depth_levels> [input_file|input_url] instead in PowerShell");
            }
            using (var cts = new CancellationTokenSource())
            using (var stream = Console.OpenStandardInput())
            {
                var result = await this.Executor!.CalculatePriceDepthSanpshotAsync(stream, cts.Token);
                await this.OutputPriceDepthSnapshot(result, this.TimeStamp);
            }
        }

        bool CheckShell()
        {
            string shell = Environment.GetEnvironmentVariable(BusinessConstant.SHELL) ?? string.Empty;
            if (!String.IsNullOrEmpty(shell) && shell.Contains(BusinessConstant.BASH))
            {
                return true;
            }
            return false;
        }
    }
}
