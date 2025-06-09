using log4net;
using OrderBookProcessor.Core;
using OrderBookProcessor.Utils;
using System.Configuration;


namespace OrderBookProcessor.TaskManagement
{
    internal abstract class BaseTaskWorker:ITaskWorker
    {
        protected string TimeStamp { get; init; } = string.Empty;

        protected string OutputPath { get; init; } = string.Empty;

        protected int DepthLevels { get; init; } = 0;

        protected IOrderBookExecutor? Executor { get; set; } = null;

        public virtual string getTimeStamp() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        public virtual string getOutputFilePath(string symbol) => symbol 
                                                    + ConfigurationManager.AppSettings[BusinessConstant.KEY_OUTPUT_FILE_Suffix] 
                                                    ?? BusinessConstant.OutputFileSuffixDefault;
        // Output Snapshot details to all the [Symbol]-price-depth-snapshot-output.txt files respectively as per their [Symbol]. The files are under output-[Timestamp] 
        public virtual async Task OutputPriceDepthSnapshot(Dictionary<string, List<string>> priceDepthSnapshot, string timeStamp)
        {
            var symbols = priceDepthSnapshot.Keys.Order();
            // If there is only one symbol, there is no need to output another file that contains all details of all the symbols
            if (symbols.Count() == 2)
            {
                symbols = priceDepthSnapshot.Keys.Where(symbol => symbol != BusinessConstant.All).Order();
            }
            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), ConfigurationManager.AppSettings["OutputDir"] + timeStamp);
            FileUtils.CreateDirectory(pathToSave);
           // symbols = priceDepthSnapshot.Keys.Where(symbol => symbol == BusinessConstant.All).Order();
            foreach (var symbol in symbols)
            {
                string fileName = this.getOutputFilePath(symbol);
                string filePath = Path.Join(pathToSave, fileName);
                List<string> values = priceDepthSnapshot[symbol];
                string content = string.Join(Environment.NewLine, values);
                await FileUtils.WriteStringToFileAsync(content, filePath);
            }
            Console.WriteLine();
            Console.WriteLine($"Kindly go to {pathToSave} to check all details of symbols. ");
            Console.WriteLine();
        }

        public virtual void setOrderBookExecutor(IOrderBookExecutor executor) => this.Executor = executor;

        public virtual void LogStartInfo(string taskType)
        {
            LogUtils.logInfo($"{taskType} picks up the task");
            LogUtils.logInfo($"Task Id is {this.TimeStamp}");
            LogUtils.logInfo($"Price Depth Level is {this.DepthLevels}");
        }
        
        public abstract Task ExecuteTask();
    }
}
