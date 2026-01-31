using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace RFMediaLinkService;

class Program
{
    static void Main(string[] args)
    {
        // Check if this is a window activation command
        if (args.Length > 0 && args[0] == "activate")
        {
            // Extract process ID and call ActivateWindow helper
            var activateArgs = new string[args.Length - 1];
            Array.Copy(args, 1, activateArgs, 0, args.Length - 1);
            ActivateWindow.Run(activateArgs);
            return;
        }

        // Normal service startup
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddEventLog(new EventLogSettings
                {
                    SourceName = "RFMediaLinkService",
                    LogName = "Application"
                });
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<RfidWorker>();
            })
            .Build()
            .Run();
    }
}
