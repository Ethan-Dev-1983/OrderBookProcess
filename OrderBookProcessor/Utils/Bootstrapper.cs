using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderBookProcessor.TaskManagement;

namespace OrderBookProcessor.Utils
{
    internal static class Bootstrapper
    {
        public static async ValueTask Start(string[] args) {
            // Check the input parameter
            LogUtils.logInfo("The program begins to run");
            // If we run the program in vistual studio, we should input the parameters manually
            args = Bootstrapper.InputMode(args);
            bool passed = Bootstrapper.CheckInputArgs(args);
            if (!passed)
            {
                return;
            }
            try
            {
                WorkerTaskFactory taskFactory = new WorkerTaskFactory(args);

                // Retrieve the particular task worker to handle the tasks according to input parameter
                var workerTask = taskFactory.DistriuteTaskWorker();

                await workerTask.ExecuteTask();
                LogUtils.logInfo("Program execution completed");
                if (args.Length == 2)
                {
                    Console.WriteLine("Press any key ...");
                    Console.ReadKey();
                }
            }
            catch (HttpRequestException ex)
            {
                LogUtils.logErrorWithConsole($"Netwrok error: {ex.Message} {ex.StackTrace}");
            }
            catch (IOException ex)
            {
                LogUtils.logErrorWithConsole($"File IO error: {ex.Message} {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                LogUtils.logErrorWithConsole($"Fatal error: {ex.Message} {ex.StackTrace}");
            }
        }

        public static string[] InputMode(string[] args)
        {
            // if we start the program in vistual studio, we should input the parameter manually
            if (args.Length == 0)
            {
                LogUtils.logInfo("The price depth level and file path were input from user ");
                string?[] inputArgs = new string[2];
                while (true)
                {
                    if (!CheckDevelPriceLevel(inputArgs[0]))
                    {
                        Console.WriteLine("Kindly input price depth level <depth_levels>: ");
                        inputArgs[0] = Console.ReadLine();
                        if (!CheckDevelPriceLevel(inputArgs[0]))
                        {
                            continue;
                        }
                    }
                    if (!CheckFilePath(inputArgs[1]))
                    {
                        Console.WriteLine("Kindly input either stream file path or net work URL: ");
                        inputArgs[1] = Console.ReadLine();
                        if (!CheckFilePath(inputArgs[1]))
                        {
                            continue;
                        }
                    }
                    break;
                   
                }
                return inputArgs!;

            }
            return args;
        }
        public static bool CheckDevelPriceLevel(string? level)
        {
            if (!int.TryParse(level, out int depthLevels) || depthLevels <= 0)
            {
                return false;
            }
            return true;
        }
        public static bool CheckFilePath(string? filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) && !string.IsNullOrEmpty(filePath); 
        }
        public static bool CheckInputArgs(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Usage: OrderBookProcessor <depth_levels> [input_file|input_url]");
                Console.WriteLine("If no input source specified, reads from stdin");
                Console.WriteLine("Supported input sources:");
                Console.WriteLine("  - Local file path (e.g., orders.stream)");
                Console.WriteLine("  - Network URL (e.g., http://example.com/orders.stream)");
                Console.WriteLine("  - Pipe for standard input in Shell Bash: cat [input_file] | OrderBookProcessor <depth_levels>");
                return false;
            }

            if (!CheckDevelPriceLevel(args[0]))
            {
                LogUtils.logErrorWithConsole("Depth levels must be a positive integer");
                return false;
            }

            if (args.Length > 1 && !CheckFilePath(args[1]))
            {
                LogUtils.logErrorWithConsole("[input_file|input_url] is either local file path or network URL");
                return false;
            }

            return true;
        }
    }
}
