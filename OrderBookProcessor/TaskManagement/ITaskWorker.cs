using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.TaskManagement
{
    internal interface ITaskWorker
    {
        string getTimeStamp();
        string getOutputFilePath(string symbol);
        Task ExecuteTask();
    }
}
