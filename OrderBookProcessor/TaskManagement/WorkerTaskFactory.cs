using OrderBookProcessor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.TaskManagement
{
    internal class WorkerTaskFactory
    {
        string[] Args {  get; init; }
        int depthLevels = 0;
        string inputSource = string.Empty;

        string InputSource { get { return this.inputSource; } }

        int DepthLevels { get { return this.depthLevels; } }
        public WorkerTaskFactory(string[] args)
        {
            this.Args = args;
            int.TryParse(args[0], out this.depthLevels);
        }
        public BaseTaskWorker DistriuteTaskWorker()
        {
            Console.WriteLine();
            if (this.Args.Length == 2)
            {
                inputSource = this.Args[1];
                if (FileUtils.IsUrl(inputSource))
                {
                    return new NetworkTaskWorker(this.DepthLevels, this.InputSource);
                }else
                {
                    return new LocalFileTaskWorker(this.DepthLevels, this.InputSource);
                }
            }
            else if (this.Args.Length == 1)
            {
                return new LocalStdInTaskWorker(this.DepthLevels);
            }
            else {
                throw new Exception("No Task Worker Object can match the input parameters.");
            }
        }
    }
}
