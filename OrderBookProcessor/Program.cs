namespace OrderBookProcessor
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using OrderBookProcessor.Utils;

    internal class Program
    {
        static async Task Main(string[] args)
        {
          
            await Bootstrapper.Start(args);
            
        }        
    }
}
