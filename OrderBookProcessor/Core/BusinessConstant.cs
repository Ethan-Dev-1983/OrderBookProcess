using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderBookProcessor.Core
{
    internal class BusinessConstant
    {
        public const string Http = "http";
        public const string Https = "http";
        public const char AddOrder = 'A';
        public const char UpdateOrder = 'U';
        public const char DeleteOrder = 'D';
        public const char ExecuteOrder = 'E';
        public const char Bid = 'B';
        public const char Sell = 'S';
        public const int DownLoadTimeout = 30;
        public const int DownLoadChunkSize = 8 * 1024;
        public const int DownLoadMaxThread = 4;
        public const string All = "All";
        public const string KEY_DOWNLOAD_TIMEOUT = "default";
        public const string KEY_OUTPUT_FILE_Suffix = "OutputFileSuffix";
        public const string OutputFileSuffixDefault = KEY_OUTPUT_FILE_Suffix;
        public const string SHELL = "SHELL";
        public const string BASH = "bash";
        
    }
}
