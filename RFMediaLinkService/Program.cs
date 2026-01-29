using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

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
            .ConfigureServices(services =>
            {
                services.AddHostedService<RfidWorker>();
            })
            .Build()
            .Run();
    }
}
