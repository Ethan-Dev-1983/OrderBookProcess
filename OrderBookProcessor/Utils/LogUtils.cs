using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OrderBookProcessor.Utils
{
    internal static class LogUtils
    {
        public static readonly ILog Log;
        static LogUtils()
        {
            var repo = LogManager.GetRepository();
            XmlConfigurator.Configure(repo, new FileInfo("log4net.config"));
            Log = LogManager.GetLogger("DefaultLogger");
        }
        static public void logError(string error) => Log.Error(error);

        static public void logInfo(string info) => Log.Info(info);
        static public void logWarnWithConsole(string warn){
            if (string.IsNullOrEmpty(warn)) return;
            Log.Warn(warn);
            Console.Error.WriteLine(warn);
        }

        static public void logErrorWithConsole(string error)
        {
            if (string.IsNullOrEmpty(error)) return;
            Log.Error(error);
            Console.Error.WriteLine(error);
        }
    }
}
